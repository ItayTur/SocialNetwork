﻿using BL.Helpers;
using Common.Dtos;
using Common.Enums;
using Common.Exceptions;
using Common.Interfaces;
using Common.Loggers;
using Common.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace BL.Managers
{
    public class AuthManager : IAuthManager
    {
        private delegate Task BlockHandler(string registrationKey);
        private readonly IAuthRepository _authRepository;
        private readonly IFacebookAuthRepository _facebookAuthRepository;
        private readonly ILoginTokenManager _loginTokenManager;
        private readonly Dictionary<RegistrationTypeEnum, BlockHandler> _blockHandlers;
        private readonly string _socialUrl;
        private readonly string _identityUrl;
        private readonly string _notificationsUrl;



        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="authRepository"></param>
        /// <param name="loginTokenManager"></param>
        public AuthManager(IAuthRepository authRepository, ILoginTokenManager loginTokenManager, IFacebookAuthRepository facebookAuthRepository)
        {
            _blockHandlers = new Dictionary<RegistrationTypeEnum, BlockHandler>();
            InitializeBlockHandlers();
            _authRepository = authRepository;
            _facebookAuthRepository = facebookAuthRepository;
            _loginTokenManager = loginTokenManager;
            _identityUrl = ConfigurationManager.AppSettings["IdentityUrl"];
            _socialUrl = ConfigurationManager.AppSettings["SocialUrl"];
            _notificationsUrl = ConfigurationManager.AppSettings["NotificationsUrl"];
        }


        /// <summary>
        /// Initialize the block handlers dictionry.
        /// </summary>
        private void InitializeBlockHandlers()
        {
            _blockHandlers.Add(RegistrationTypeEnum.Facebook, BlockFacebookAuth);
            _blockHandlers.Add(RegistrationTypeEnum.UserNamePassword, BlockUserNamePasswordAuth);
        }



        /// <summary>
        /// Blockes users registerd with facebook.
        /// </summary>
        /// <param name="registrationKey"></param>
        /// <returns></returns>
        private async Task BlockFacebookAuth(string registrationKey)
        {
            try
            {
                FacebookAuthModel facebookAuth = _facebookAuthRepository.GetAuthByFacebookId(registrationKey);
                facebookAuth.IsBLocked = true;
                await _facebookAuthRepository.Update(facebookAuth);
            }
            catch (Exception)
            {

                throw;
            }
        }



        /// <summary>
        /// Blockes users registerd with username and password.
        /// </summary>
        /// <param name="registrationKey"></param>
        /// <returns></returns>
        private async Task BlockUserNamePasswordAuth(string registrationKey)
        {
            try
            {
                AuthModel authModel = _authRepository.GetAuthByEmail(registrationKey);
                authModel.IsBLocked = true;
                await _authRepository.Update(authModel);
            }
            catch (Exception e)
            {

                throw e;
            }
        }



        /// <summary>
        /// Activates the system block on the user associated with the specified Id.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="blockedId"></param>
        /// <returns></returns>
        public async Task BlockUser(string token, string blockedId)
        {
            try
            {
                await _loginTokenManager.VerifyAsync(token);
                UserModel userToBlock = await GetUserById(token, blockedId);
                Enum.TryParse(userToBlock.RegistrationType, out RegistrationTypeEnum registrationType);
                await _blockHandlers[registrationType](userToBlock.RegistrationKey);
            }
            catch (Exception e)
            {

                throw e;
            }
        }



        /// <summary>
        /// Gets the user associated with the speci
        /// </summary>
        /// <param name="token"></param>
        /// <param name="blockedId"></param>
        /// <returns></returns>
        private async Task<UserModel> GetUserById(string token, string blockedId)
        {
            try
            {
                JObject dataToSend = new JObject();
                dataToSend.Add("token", token);
                dataToSend.Add("userId", blockedId);
                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.PostAsJsonAsync(_identityUrl + "/GetUserById", dataToSend);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsAsync<UserModel>();
                    }
                    else
                    {
                        throw new Exception("couldn't connect to identity server");
                    }
                }
            }
            catch (Exception e)
            {

                throw e;
            }
        }



        /// <summary>
        /// Gets the email of the user associated with the specified id.
        /// </summary>
        /// <param name="token"></param>
        /// <param name="blockedId"></param>
        /// <returns></returns>
        private async Task<string> GetUserEmailById(string token, string userId)
        {
            try
            {
                string emailToReturn = "";
                JObject dataToSend = new JObject();
                dataToSend.Add("token", token);
                dataToSend.Add("userId", userId);
                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.PostAsJsonAsync(_identityUrl + "/GetUserEmailById", dataToSend);
                    if (response.IsSuccessStatusCode)
                    {
                        emailToReturn = await response.Content.ReadAsAsync<string>();
                    }
                    else
                    {
                        throw new Exception("couldn't connect to identity server");
                    }

                    return emailToReturn;
                }
            }
            catch (Exception)
            {

                throw;
            }
        }



        /// <summary>
        /// Changes the user password.
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="currentPassword"></param>
        /// <param name="oldPassword"></param>
        /// <returns></returns>
        public async Task ResetPassword(string token, HttpRequest httpRequest)
        {
            try
            {
                string userId = await _loginTokenManager.VerifyAsync(token);
                string userEmail = await GetUserEmailById(token, userId);
                AuthModel authModel = _authRepository.GetAuthByEmail(userEmail);
                string oldPassword = httpRequest["OldPassword"];
                string newPassword = httpRequest["NewPassword"];
                VerifyAuthPassword(authModel, oldPassword);
                authModel.Password = SecurePasswordHasher.Hash(newPassword);
                await _authRepository.Update(authModel);
            }
            catch (Exception e)
            {

                throw e;
            }
        }
        


        /// <summary>
        /// Logins the user associated with the specified email and password.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<string> LoginUser(string email, string password)
        {
            try
            {
                var auth = _authRepository.GetAuthByEmail(email);
                if (!auth.IsBLocked)
                {
                    VerifyAuthPassword(auth, password);
                    return await _loginTokenManager.Add(auth.UserId, LoginTokenModel.LoginTypes.UserPassword);
                }
                throw new UserBlockedException("user is blocked");

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }



        /// <summary>
        /// Registers the email and the password to the auth table.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns>The access token associated with the specified email and password.</returns>
        public async Task<string> RegisterUserAndLogin(RegistrationDto registrationDto)
        {
            try
            {
                VerifyEmailIsUnique(registrationDto.Email);
                string userId = GenerateUserId();
                var appToken = await _loginTokenManager.Add(userId, LoginTokenModel.LoginTypes.UserPassword);
                await AddUserToDatabases(registrationDto, userId, appToken);
                return appToken;
            }
            catch (DuplicateKeyException ex)
            {
                LoggerFactory.GetInstance().AllLogger().Log(ex.Message);
                throw ex;
            }
            catch (Exception ex)
            {
                LoggerFactory.GetInstance().AllLogger().Log(ex.Message);
                throw ex;
            }
        }



        /// <summary>
        /// Addes the user to the Users table and the email to the Auth table.
        /// </summary>
        /// <param name="registrationDto"></param>
        /// <param name="userEmail"></param>
        /// <param name="appToken"></param>
        private async Task AddUserToDatabases(RegistrationDto registrationDto, string userId, string appToken)
        {
            try
            {
                Task addUserTask = AddUserToUsersDb(appToken, registrationDto, userId);
                Task addAuthTask = AddUserToAuthDb(registrationDto.Email, SecurePasswordHasher.Hash(registrationDto.Password), userId);

                Task addUserNodeTask = AddUserToGraphDb(appToken, registrationDto.Email, registrationDto.FirstName + " " + registrationDto.LastName);
                Task addUserToNotificationTask = AddUserToNotificationsDb(appToken);

                await Task.WhenAll(addUserTask, addAuthTask, addUserNodeTask, addUserToNotificationTask);

            }
            catch (AggregateException ae)
            {
                Task removeUserTaskawait = RemoveIdentity(appToken);
                Task removeAuthTaskawait = RemoveAuthFromDb(userId);
                Task removeUserToGraphTask = RemoveGraphNode(appToken);
                Task removeUserToNotificationTask = RemoveNotificationsAuth(appToken);

                await Task.WhenAll(removeUserTaskawait, removeAuthTaskawait, removeUserToGraphTask, removeUserToNotificationTask);

                throw new Exception("Internal server error");
            }

        }



        /// <summary>
        /// Addes user to graph database.
        /// </summary>
        /// <param name="appToken"></param>
        /// <param name="facebookUserDto"></param>
        /// <returns></returns>
        private async Task AddUserToGraphDb(string appToken, string email, string name)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var dataToSend = new JObject
                    {
                        { "token", JToken.FromObject(appToken) },
                        { "email", JToken.FromObject(email) },
                        { "name", JToken.FromObject(name) }
                    };
                    var response = await httpClient.PostAsJsonAsync(_socialUrl + "Users/AddUser", dataToSend);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new AddUserToGraphException();
                    }
                }
            }
            catch (AddUserToGraphException e)
            {

                throw e;
            }
            catch (Exception e)
            {
                throw new AddUserToGraphException(e.Message);
            }

        }



        /// <summary>
        /// Addes user to Notifications database.
        /// </summary>
        /// <param name="appToken"></param>
        /// <param name="facebookUserDto"></param>
        /// <returns></returns>
        private async Task AddUserToNotificationsDb(string appToken)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.PostAsJsonAsync(_notificationsUrl + "Notifications/Register", new AccessTokenDto() { Token = appToken });
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Couldn't connect to notifications server");
                    }
                }
            }
            catch (HttpRequestException e)
            {
                throw new AddUserToXMPPDbException(e.Message);
            }
            catch (Exception e)
            {
                throw new AddUserToXMPPDbException(e.Message);
            }
        }



        /// <summary>
        /// Removes the user associated with the specified token from the database.
        /// </summary>
        /// <param name="token"></param>
        private async Task RemoveIdentity(string token)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.DeleteAsync(_identityUrl + $"/DeleteUser/{token}");
                if (!response.IsSuccessStatusCode)
                {
                    throw new ArgumentException("Identity server could not remove the user");
                }
            }
        }



        /// <summary>
        /// Removes the user associated with the specified id from the graph DB.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        private async Task RemoveGraphNode(string token)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var response = await httpClient.DeleteAsync(_socialUrl + $"users/DeleteUserByToken/{token}");
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Error with removing from the graph");
                    }
                }
            }
            catch (Exception e)
            {

                throw e;
            }

        }



        /// <summary>
        /// Removes the user associated with the specified token from the database.
        /// </summary>
        /// <param name="token"></param>
        private async Task RemoveNotificationsAuth(string token)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.DeleteAsync(_notificationsUrl + $"/DeleteUser/{token}");
                if (!response.IsSuccessStatusCode)
                {
                    throw new ArgumentException("Notifications server could not remove the user");
                }
            }
        }



        /// <summary>
        /// Removes the user associated with the specified email from the database.
        /// </summary>
        /// <param name="userId"></param>
        private async Task RemoveUserFromDb(string userId)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.DeleteAsync(_identityUrl + $"/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    throw new ArgumentException("Identity server could not remove the user");
                }
            }
        }



        /// <summary>
        /// Removes the auth associated with the specified email from the database.
        /// </summary>
        /// <param name="email"></param>
        private async Task RemoveAuthFromDb(string email)
        {
            await _authRepository.Delete(email);
        }



        /// <summary>
        /// Addes mail to the auth table in the database.
        /// </summary>
        /// <param name="userEmail"></param>
        /// <returns></returns>
        private async Task AddUserToAuthDb(string email, string password, string userId)
        {
            try
            {
                await _authRepository.Add(new AuthModel(email, password, userId));
            }
            catch (Exception e)
            {
                //TODO: log
                throw new AddAuthToDbException(e.Message);
            }
        }



        /// <summary>
        /// Adds a user entity to the users database through the identity service.
        /// </summary>
        /// <param name="appToken"></param>
        /// <param name="user"></param>
        private async Task AddUserToUsersDb(string appToken, RegistrationDto registrationDto, string userId)
        {
            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    var user = new UserModel()
                    {
                        Id = userId,
                        FirstName = registrationDto.FirstName,
                        LastName = registrationDto.LastName,
                        Email = registrationDto.Email,
                        Address = registrationDto.Address,
                        Job = registrationDto.Job,
                        BirthDate = registrationDto.BirthDate,
                        RegistrationType = RegistrationTypeEnum.UserNamePassword.ToString(),
                        RegistrationKey = registrationDto.Email
                    };
                    var data = new JObject();
                    data.Add("user", JToken.FromObject(user));
                    data.Add("token", JToken.FromObject(appToken));
                    var response = await httpClient.PostAsJsonAsync(_identityUrl, data);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Identity server could not add the user");
                    }
                }
            }
            catch (Exception e)
            {
                //TODO: log
                throw new AddUserToDbException(e.Message);
            }

        }

        

        /// <summary>
        /// Verfies the email occupation. Throws an exception other wise.
        /// </summary>
        /// <param name="email"></param>
        private void VerifyEmailIsUnique(string email)
        {
            if (!_authRepository.IsEmailFree(email))
            {
                throw new DuplicateKeyException(email, "Email already exists");
            }
        }

               

        /// <summary>
        /// Verfies the auth password. Throws an exception if not valid.
        /// </summary>
        /// <param name="auth"></param>
        /// <param name="password"></param>
        private void VerifyAuthPassword(AuthModel auth, string password)
        {
            if (auth == null || !SecurePasswordHasher.Verify(password, auth.Password))
            {
                throw new PasswordException();
            }
        }



        /// <summary>
        /// Genrates unique Id.
        /// </summary>
        /// <returns></returns>
        private string GenerateUserId()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
