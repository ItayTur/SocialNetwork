import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { retry, catchError, map } from 'rxjs/operators';
import { ErrorHandlingService } from './error-handling.service';
import { UserModel } from './user.model';


@Injectable({
  providedIn: 'root'
})
export class TagsService {

  constructor(private httpClient: HttpClient, private errorHandler: ErrorHandlingService) { }

  GetTags(text: string): Observable<any> {

    const url = "http://localhost:4573/api/Posts/SearchTag/"+text;
    return this.httpClient.get<UserModel[]>(url, { withCredentials: true })
    .pipe(retry(3), catchError(this.errorHandler.handleError));
  }
}
