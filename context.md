# Backend Context - UniKnowledge System

Tài liệu tóm lược cho AI Coding Assistant nhằm hỗ trợ phát triển và duy trì mã nguồn Backend dự án UniKnowledge.

## 1. Project Overview
- **Mục tiêu**: Nền tảng chia sẻ kiến thức (Q&A) tương tự StackOverflow dành cho sinh viên.
- **Tính năng cốt lõi**: Quản lý câu hỏi/câu trả lời, bình chọn (Voting), Phân loại theo Danh mục & Tag, Chat thời gian thực.
- **Đối tượng**: Sinh viên, Admin quản trị hệ thống.

## 2. Tech Stack
- **Ngôn ngữ**: C# (.NET 9.0 Web API).
- **OR/M**: Entity Framework Core 9.0.
- **Database**: SQL Server (LocalDB).
- **Real-time**: SignalR.
- **Security**: JWT Bearer Authentication, BCrypt Hash Password.
- **Thư viện chính**: `MailKit` (Email), `DotNetEnv` (Env), `Newtonsoft.Json`.

## 3. Core Architecture
- **Pattern**: **Service Layer Implementation**.
- **Luồng xử lý**: `Controllers` -> `Interfaces` (Dependency Injection) -> `Services` (Business Logic) -> `AppDbContext` (Data Access).
- **Standard**: Sử dụng `DTOs` để nhận/trả dữ liệu thay vì Entity trực tiếp.

## 4. Key Features & API Logic
- **Auth & Security**: Cấp OTP qua Email để reset mật khẩu, JWT Token có thời gian 24h.
- **Question Logic**: Phân trang theo **Cursor** (CreatedAt + Id) để tối ưu performance cho bộ dữ liệu lớn. Tự động tăng ViewCount khi xem chi tiết.
- **Answer Logic**: Chủ câu hỏi được chọn "Accepted Answer" duy nhất.
- **Tag & Category**: Quản lý quan hệ Many-to-Many giữa Question và Tag. Seeding mặc định đầy đủ Danh mục kỹ thuật.
- **Real-time Chat**: Hub SignalR xử lý Message, thông báo Typing và trạng thái đã đọc (Read).

## 5. Database Schema (Core Entities)
- **User**: (1-N) Question, Answer, Vote, Message.
- **Question**: (FK) User, Category; (1-N) Answer, Vote; (M-N) Tag.
- **Answer**: (FK) Question, User.
- **Tag**: (M-N) Question.
- **Category**: (1-N) Question.

## 6. Development Rules
- **Naming**: PascalCase cho Class/Method, camelCase cho local variables.
- **Services Pattern**: Mỗi Controller PHẢI có 1 Interface tương ứng (ví dụ: `ITagService`, `ICategoryService`).
- **Error Handling**: Ưu tiên trả về **Tuple** `(bool Success, string Message, ...)` trong các Service để Controller dễ dàng map sang HTTP Status (400, 404, 200).
- **Standard DTOs**: Sử dụng DTO chuyên biệt cho từng tác vụ (CreateDTO, UpdateDTO, ResponseDTO).
- **Logic**: Không viết logic xử lý dữ liệu (LINQ, DB) trực tiếp bên trong Controller.
