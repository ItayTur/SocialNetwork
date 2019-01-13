﻿using Amazon;
using Amazon.S3;
using Common;
using Common.Dtos;
using Common.Interfaces;
using Common.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BL.Managers
{
    public class PostsManager : IPostsManager
    {

        private readonly IPostsRepository _postsRepository;
        private readonly IStorageManager _storageManager;

        public PostsManager(IPostsRepository postsRepository, IStorageManager storageManager)
        {
            _postsRepository = postsRepository;
            _storageManager = storageManager;
        }

        /// <summary>
        /// Adds a post to the database. 
        /// </summary>
        /// <param name="posterId"></param>
        /// <param name="post"></param>
        /// <param name="tagIds"></param>
        public async Task Add(HttpRequest httpRequest, string token, string path)
        {
            try
            {

                PostModel post = new PostModel()
                {
                    Content = httpRequest["Content"],
                    DateTime = DateTime.Now
                };

                var picFile = httpRequest.Files["pic"];

                var userId = await VerifyToken(token);
                post.Id = GenerateId();
                post.ImgUrl = await _storageManager.AddPicToStorage(picFile, path).ConfigureAwait(false);
                var addPostToDbTask = _postsRepository.Add("posting-user-id", post);
            }
            catch (Exception e)
            {

                throw new Exception(e.Message);
            }
        }

        

        private string GenerateId()
        {
            return Guid.NewGuid().ToString();
        }

        private async Task<string> VerifyToken(string token)
        {
            TokenDto tokenDto = new TokenDto() { Token = token };
            using(HttpClient httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsJsonAsync("localHost:53535", token);
                if (!response.IsSuccessStatusCode)
                {
                    throw new AuthenticationException();
                }
                else
                {
                    return await response.Content.ReadAsAsync<string>();
                }
            }
        }
    }
}
