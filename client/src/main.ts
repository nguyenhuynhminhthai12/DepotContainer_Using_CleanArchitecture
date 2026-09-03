/**
 * Điểm khởi đầu (Bootstrap) của ứng dụng Angular.
 * Khởi tạo ứng dụng với AppComponent và appConfig đã đăng ký.
 * Bản quyền (c) 2026 TechSpherex.
 */
import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';

bootstrapApplication(AppComponent, appConfig).catch((err) => console.error(err));
