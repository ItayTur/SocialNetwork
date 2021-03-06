import { BrowserModule } from '@angular/platform-browser';
import { NgModule, NO_ERRORS_SCHEMA } from '@angular/core';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule } from '@angular/common/http';
import { MatSnackBarModule } from '@angular/material';

import { TagInputModule } from 'ngx-chips';

import { AppComponent } from './app.component';

import { MDBBootstrapModule } from 'angular-bootstrap-md';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { LoginComponent } from './login/login.component';
import { NewsFeedComponent } from './news-feed/news-feed.component';
import { PostAddingComponent } from './post-adding/post-adding.component';

import { FacebookModule } from 'ngx-facebook';
import { CookieService } from 'ngx-cookie-service';
import { CoreModule } from './core/core.module';
import { AppRoutingModule } from './app-routing.module';
import { PageNotFoundComponent } from './page-not-found/page-not-found.component';
import { UserInfoComponent } from './user-info/user-info.component';
import { RegisterComponent } from './register/register.component';
import { PostsComponent } from './posts/posts.component';
import { PostComponent } from './post/post.component';
import { NotificationsComponent } from './notifications/notifications.component';
import { CommentComponent } from './comment/comment.component';
import { AddCommentComponent } from './add-comment/add-comment.component';
import { TagsComponent } from './tags/tags.component';
import { UsersComponent } from './users/users.component';
import { UserComponent } from './user/user.component';
import { UserUpdateComponent } from './user-update/user-update.component';
import { PasswordResetComponent } from './password-reset/password-reset.component';

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    NewsFeedComponent,
    PageNotFoundComponent,
    PostAddingComponent,
    UserInfoComponent,
    RegisterComponent,
    PostsComponent,
    PostComponent,
    NotificationsComponent,
    CommentComponent,
    AddCommentComponent,
    TagsComponent,
    UsersComponent,
    UserComponent,
    UserUpdateComponent,
    PasswordResetComponent

  ],
  imports: [
    FacebookModule.forRoot(),
    BrowserModule,
    BrowserAnimationsModule,
    MDBBootstrapModule.forRoot(),
    FormsModule,
    CoreModule,
    HttpClientModule,
    ReactiveFormsModule,
    MatSnackBarModule,
    AppRoutingModule,
    TagInputModule
  ],
  providers: [CookieService],
  bootstrap: [AppComponent],
  schemas: [ NO_ERRORS_SCHEMA ]
})
export class AppModule { }
