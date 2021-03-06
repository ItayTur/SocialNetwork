import { Injectable } from '@angular/core';
import { HttpClient } from "@angular/common/http";
import { Observable } from 'rxjs';
import { retry, catchError } from 'rxjs/operators';
import { ErrorHandlingService } from "./error-handling.service";

@Injectable({
  providedIn: 'root'
})
export class PostsService {

  baseUrl = "http://localhost:4573/api/Posts";
  constructor(private httpClient: HttpClient, private errorHandler: ErrorHandlingService) { }

  GetPosts(): Observable<any> {
    return this.httpClient.get(this.baseUrl+"/GetUsersPosts", { withCredentials: true })
    .pipe(retry(3), catchError(this.errorHandler.handleError));
  }

  LikePost(formData: FormData): Observable<any> {
    return this.httpClient.post(this.baseUrl+"/LikePost", formData, { withCredentials: true })
    .pipe(catchError(this.errorHandler.handleError));
  }

  IsPostLikedBy(formData: FormData): Observable<any> {
    return this.httpClient.post(this.baseUrl+"/IsPostLikedBy", formData, { withCredentials: true })
    .pipe(catchError(this.errorHandler.handleError));
  }

  UnLikePost(formData: FormData) {
    return this.httpClient.post(this.baseUrl+"/UnLikePost",formData,{ withCredentials: true })
    .pipe(retry(3), catchError(this.errorHandler.handleError));
  }

  AddComment(formData: FormData) {
    return this.httpClient.post(this.baseUrl+'/AddComment', formData, {withCredentials: true})
    .pipe(retry(3), catchError(this.errorHandler.handleError));
  }

  GetComments(postId: string): Observable<any> {
    return this.httpClient.get(this.baseUrl+"/GetCommentsOfPost/"+postId, { withCredentials: true })
    .pipe(catchError(this.errorHandler.handleError));
  }
}
