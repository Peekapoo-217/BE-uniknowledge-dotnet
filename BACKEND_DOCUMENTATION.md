# UniKnowledge Backend - Tài Liệu Hệ Thống

> **Framework:** ASP.NET Core Web API (.NET 9.0)  
> **Database:** SQL Server (LocalDB) + Entity Framework Core 9.0  
> **Authentication:** JWT Bearer Token  
> **Real-time:** SignalR  
> **Project:** `UniKnowledge.sln`

---

## 📁 Cấu Trúc Thư Mục

```
BE/UniKnowledge/
├── Controllers/          # API Controllers (10 files)
│   ├── AdminController.cs
│   ├── AnswersController.cs
│   ├── AuthController.cs
│   ├── CategoriesController.cs
│   ├── MessagesController.cs
│   ├── QuestionsController.cs
│   ├── TagsController.cs
│   ├── UploadController.cs
│   ├── UserProfileController.cs
│   └── VotesController.cs
├── DTOs/                 # Data Transfer Objects (8 folders)
│   ├── Answer/           # AnswerResponseDto, CreateAnswerDto, UpdateAnswerDto
│   ├── Auth/             # AuthResponseDto, LoginDto, RegisterDto, PasswordResetDtos
│   ├── Category/         # CategoryDto, CreateCategoryDto, UpdateCategoryDto
│   ├── Message/          # MessageResponseDto, ConversationDto, CreateMessageDto, TypingIndicatorDto
│   ├── Question/         # QuestionResponseDto, QuestionSummaryDto, CreateQuestionDto, UpdateQuestionDto
│   ├── Tag/              # TagResponseDto, CreateTagDto, UpdateTagDto, TagFilterDto
│   ├── User/             # UserProfileDtos (UserProfileDto, UpdateProfileDto, ChangePasswordDto)
│   └── Vote/             # VoteDto
├── Data/                 # Database Context
│   └── AppDbContext.cs
├── Hubs/                 # SignalR Hubs
│   └── ChatHub.cs
├── Migrations/           # EF Core Migrations
├── Models/               # Entity Models (9 files)
│   ├── Answer.cs
│   ├── Category.cs
│   ├── Message.cs
│   ├── PasswordResetToken.cs
│   ├── Question.cs
│   ├── QuestionTag.cs
│   ├── Tag.cs
│   ├── User.cs
│   └── Vote.cs
├── Services/             # Business Logic (12 files)
│   ├── AnswerService.cs
│   ├── AuthService.cs
│   ├── CategoryService.cs
│   ├── EmailService.cs
│   ├── FileUploadService.cs
│   ├── JwtService.cs
│   ├── MessageService.cs
│   ├── PasswordResetService.cs
│   ├── QuestionService.cs
│   ├── TagService.cs
│   ├── UserProfileService.cs
│   └── VoteService.cs
├── Settings/             # Configuration Classes
│   ├── EmailSettings.cs
│   └── PasswordResetSettings.cs
├── wwwroot/              # Static Files (uploads)
├── Program.cs            # Application Entry Point
├── appsettings.json      # Configuration
└── UniKnowledge.csproj   # Project File
```

---

## 📦 NuGet Packages

| Package | Version | Mục đích |
|---------|---------|----------|
| `BCrypt.Net-Next` | 4.0.3 | Hash & verify mật khẩu |
| `DotNetEnv` | 3.1.1 | Đọc biến môi trường từ `.env` |
| `MailKit` | 4.14.1 | Gửi email (SMTP) |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 9.0.0 | JWT authentication |
| `Microsoft.AspNetCore.OpenApi` | 9.0.4 | OpenAPI/Swagger |
| `Microsoft.EntityFrameworkCore.SqlServer` | 9.0.0 | SQL Server provider |
| `Microsoft.EntityFrameworkCore.Tools` | 9.0.0 | EF Core CLI tools |

---

## 🗄️ Database Schema (Entity Models)

### User
| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `UserId` | int | PK, Auto | ID người dùng |
| `Username` | string | Required, Max 50, Unique | Tên đăng nhập |
| `Email` | string | Required, Max 100, Unique | Email |
| `PasswordHash` | string | Required, Max 255 | Mật khẩu đã hash (BCrypt) |
| `FullName` | string? | Max 100 | Họ tên |
| `AvatarUrl` | string? | Max 255 | URL ảnh đại diện |
| `Role` | string | Required, Max 20, Default: "Student" | Vai trò: Student / Admin |
| `CreatedAt` | DateTime | Default: UtcNow | Ngày tạo |
| `UpdatedAt` | DateTime? | | Ngày cập nhật |

**Navigation:** Questions, Answers, Votes, SentMessages, ReceivedMessages

### Question
| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `QuestionId` | int | PK, Auto | ID câu hỏi |
| `UserId` | int | FK → User (Cascade) | Người đặt câu hỏi |
| `CategoryId` | int | FK → Category (Restrict) | Danh mục |
| `Title` | string | Required, Max 255 | Tiêu đề |
| `Content` | string | Required | Nội dung |
| `ViewCount` | int | Default: 0 | Lượt xem |
| `Status` | string | Required, Max 20, Default: "Open" | Trạng thái: Open / Closed / Hidden |
| `ImageUrl` | string? | Max 255 | URL ảnh đính kèm |
| `FileUrl` | string? | Max 255 | URL file đính kèm |
| `CreatedAt` | DateTime | Default: UtcNow | Ngày tạo |
| `UpdatedAt` | DateTime? | | Ngày cập nhật |

**Navigation:** User, Category, Answers, Votes, QuestionTags

### Answer
| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `AnswerId` | int | PK, Auto | ID câu trả lời |
| `QuestionId` | int | FK → Question (Cascade) | Câu hỏi |
| `UserId` | int | FK → User (Restrict) | Người trả lời |
| `Content` | string | Required | Nội dung |
| `IsAccepted` | bool | Default: false | Đã được chấp nhận |
| `CreatedAt` | DateTime | Default: UtcNow | Ngày tạo |
| `UpdatedAt` | DateTime? | | Ngày cập nhật |

**Navigation:** Question, User, Votes

### Vote
| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `VoteId` | int | PK, Auto | ID vote |
| `UserId` | int | FK → User (Cascade) | Người vote |
| `QuestionId` | int? | FK → Question (Restrict) | Câu hỏi (nullable) |
| `AnswerId` | int? | FK → Answer (Restrict) | Câu trả lời (nullable) |
| `VoteType` | int | Required, Default: 0 | 1: Upvote, -1: Downvote |
| `CreatedAt` | DateTime | Default: UtcNow | Ngày tạo |

**Unique Index:** (UserId, QuestionId) WHERE QuestionId IS NOT NULL  
**Unique Index:** (UserId, AnswerId) WHERE AnswerId IS NOT NULL

### Category
| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `CategoryId` | int | PK, Auto | ID danh mục |
| `CategoryName` | string | Required, Max 100 | Tên danh mục |
| `Description` | string? | Max 255 | Mô tả |
| `Slug` | string | Required, Max 100 | URL-friendly slug |

### Tag
| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `TagId` | int | PK, Auto | ID tag |
| `TagName` | string | Required, Max 50, Unique | Tên tag |
| `Description` | string? | Max 255 | Mô tả |

### QuestionTag (Many-to-Many)
| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `QuestionId` | int | PK, FK → Question (Cascade) | Câu hỏi |
| `TagId` | int | PK, FK → Tag (Cascade) | Tag |

### Message
| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `MessageId` | int | PK, Auto | ID tin nhắn |
| `SenderId` | int | FK → User (Restrict) | Người gửi |
| `ReceiverId` | int | FK → User (Restrict) | Người nhận |
| `Content` | string | Required | Nội dung |
| `IsRead` | bool | Default: false | Đã đọc |
| `CreatedAt` | DateTime | Default: UtcNow | Thời gian gửi |

### PasswordResetToken
| Property | Type | Constraints | Mô tả |
|----------|------|-------------|-------|
| `Id` | int | PK, Auto | ID |
| `UserId` | int | FK → User (Cascade) | Người dùng |
| `Email` | string | Required, Max 100, Indexed | Email |
| `OtpCode` | string | Required, Max 255 | OTP đã hash (BCrypt) |
| `ExpiresAt` | DateTime | Indexed | Thời gian hết hạn |
| `IsUsed` | bool | | Đã sử dụng |
| `CreatedAt` | DateTime | | Ngày tạo |

---

## 🔌 API Endpoints

### 1. AuthController — `api/auth`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `POST` | `/api/auth/register` | ❌ | Đăng ký tài khoản |
| `POST` | `/api/auth/login` | ❌ | Đăng nhập |
| `POST` | `/api/auth/forgot-password` | ❌ | Gửi OTP qua email |
| `POST` | `/api/auth/reset-password` | ❌ | Đặt lại mật khẩu bằng OTP |

**Register Request:** `{ username, email, password, fullName? }`  
**Login Request:** `{ email, password }`  
**Response:** `{ token, refreshToken, user: { userId, username, email, fullName?, avatarUrl?, role } }`

**Chi tiết xử lý:**
- Mật khẩu hash bằng BCrypt
- JWT token có thời hạn 24 giờ
- Claims: NameIdentifier (UserId), Name (Username), Email, Role
- OTP 6 ký tự, hết hạn sau 5 phút, hash bằng BCrypt
- Email gửi qua SMTP (Gmail)

---

### 2. QuestionsController — `api/questions`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `GET` | `/api/questions` | ❌ | Danh sách câu hỏi (search, filter, paging) |
| `GET` | `/api/questions/{id}` | ❌ | Chi tiết câu hỏi + tăng view |
| `POST` | `/api/questions` | ✅ | Tạo câu hỏi mới |
| `PUT` | `/api/questions/{id}` | ✅ | Cập nhật (chỉ owner) |
| `DELETE` | `/api/questions/{id}` | ✅ | Xóa (chỉ owner) |
| `POST` | `/api/questions/seed` | ✅ Admin | Seed 8 câu hỏi mẫu |

**Query Parameters (GET list):**
- `search` — Tìm trong Title và Content
- `categoryId` — Lọc theo danh mục
- `tagId` — Lọc theo tag
- `status` — Lọc theo trạng thái (mặc định ẩn Hidden)
- `page` — Trang (default: 1)
- `pageSize` — Số mục/trang (default: 20)

**Create Request:** `{ title, content, categoryId, tagIds[], imageUrl?, fileUrl? }`  
**Update Request:** `{ title?, content?, categoryId?, tagIds?, imageUrl?, fileUrl?, status? }`

**Xử lý xóa cascade:** Xóa vote câu trả lời → Xóa câu trả lời → Xóa QuestionTags → Xóa vote câu hỏi → Xóa câu hỏi → Xóa file đính kèm

---

### 3. AnswersController — `api`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `GET` | `/api/questions/{questionId}/answers` | ❌ | Danh sách câu trả lời |
| `POST` | `/api/questions/{questionId}/answers` | ✅ | Tạo câu trả lời |
| `PUT` | `/api/answers/{id}` | ✅ | Cập nhật (chỉ owner) |
| `DELETE` | `/api/answers/{id}` | ✅ | Xóa (chỉ owner) |
| `PUT` | `/api/answers/{id}/accept` | ✅ | Chấp nhận câu trả lời (chỉ chủ câu hỏi) |

**Sắp xếp:** Accepted trước → Mới nhất  
**Accept logic:** Bỏ chấp nhận các câu trả lời khác → Chấp nhận câu được chọn

---

### 4. VotesController — `api`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `POST` | `/api/questions/{id}/vote` | ✅ | Vote câu hỏi |
| `POST` | `/api/answers/{id}/vote` | ✅ | Vote câu trả lời |
| `DELETE` | `/api/questions/{id}/vote` | ✅ | Xóa vote câu hỏi |
| `DELETE` | `/api/answers/{id}/vote` | ✅ | Xóa vote câu trả lời |

**Request:** `{ voteType }` — `1` (upvote) hoặc `-1` (downvote)  
**Logic:** Nếu đã vote → cập nhật VoteType; Chưa vote → tạo mới

---

### 5. CategoriesController — `api/categories`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `GET` | `/api/categories` | ❌ | Danh sách (kèm QuestionCount) |
| `GET` | `/api/categories/{id}` | ❌ | Chi tiết danh mục |
| `POST` | `/api/categories` | ✅ Admin | Tạo danh mục |
| `PUT` | `/api/categories/{id}` | ✅ Admin | Cập nhật |
| `DELETE` | `/api/categories/{id}` | ✅ Admin | Xóa (không có câu hỏi) |
| `POST` | `/api/categories/seed` | ✅ Admin | Seed 8 danh mục mặc định |

**8 danh mục mặc định:** Programming, Web Development, Database, DevOps, Mobile Development, Data Science, Security, Other

---

### 6. TagsController — `api/tags`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `GET` | `/api/tags` | ❌ | Danh sách tags (search) |
| `GET` | `/api/tags/{id}` | ❌ | Chi tiết tag |
| `GET` | `/api/tags/popular` | ❌ | Tags phổ biến nhất |
| `GET` | `/api/tags/trending` | ❌ | Tags trending (N ngày) |
| `GET` | `/api/tags/suggest` | ❌ | Gợi ý tags (autocomplete, min 2 char) |
| `GET` | `/api/tags/{id}/questions` | ❌ | Câu hỏi theo tag |
| `POST` | `/api/tags/questions/filter` | ❌ | Lọc câu hỏi theo nhiều tags |
| `POST` | `/api/tags` | ✅ Admin | Tạo tag |
| `PUT` | `/api/tags/{id}` | ✅ Admin | Cập nhật tag |
| `DELETE` | `/api/tags/{id}` | ✅ Admin | Xóa tag |
| `POST` | `/api/tags/seed` | ✅ Admin | Seed tags mặc định |

**Filter Request:** `{ tagIds[], logic? ("AND"/"OR"), page, pageSize }`  
**Trending:** Đếm câu hỏi mới trong N ngày gần đây (default: 7)

---

### 7. MessagesController — `api/messages`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `GET` | `/api/messages/conversations` | ✅ | Danh sách hội thoại |
| `GET` | `/api/messages/conversation/{otherUserId}` | ✅ | Tin nhắn với user (paging) |
| `GET` | `/api/messages/unread-count` | ✅ | Số tin chưa đọc |
| `POST` | `/api/messages` | ✅ | Gửi tin nhắn |
| `POST` | `/api/messages/{messageId}/read` | ✅ | Đánh dấu đã đọc |

**Conversation Response:** `{ otherUserId, otherUsername, otherAvatarUrl?, lastMessage?, lastMessageTime?, unreadCount, isLastMessageFromMe }`

---

### 8. UserProfileController — `api/userprofile`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `GET` | `/api/userprofile/me` | ✅ | Hồ sơ cá nhân |
| `GET` | `/api/userprofile/me/questions` | ✅ | Câu hỏi của mình |
| `PUT` | `/api/userprofile/me` | ✅ | Cập nhật hồ sơ |
| `PUT` | `/api/userprofile/me/change-password` | ✅ | Đổi mật khẩu |
| `POST` | `/api/userprofile/me/upload-avatar` | ✅ | Upload avatar |
| `GET` | `/api/userprofile/{id}` | ❌ | Xem hồ sơ user khác |
| `GET` | `/api/userprofile/{id}/questions` | ❌ | Câu hỏi user khác |
| `GET` | `/api/userprofile/search` | ✅ | Tìm kiếm users |

**Upload Avatar:** Max 2MB, cho phép jpg/jpeg/png/gif, lưu tại `wwwroot/uploads/avatars/`  
**Search:** Theo username, fullName, email (loại trừ user hiện tại)

---

### 9. UploadController — `api/upload`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `POST` | `/api/upload/question-attachment` | ✅ | Upload file đính kèm |
| `DELETE` | `/api/upload/question-attachment` | ✅ | Xóa file đính kèm |

**Cho phép:** `.pdf`, `.doc`, `.docx`, `.txt`, `.zip`, `.rar`, `.jpg`, `.jpeg`, `.png`, `.gif`  
**Giới hạn:** Max 10MB  
**Lưu trữ:** `wwwroot/uploads/questions/{year}/{month}/{guid}.{ext}`  
**Bảo mật:** Kiểm tra path nằm trong wwwroot

---

### 10. AdminController — `api/admin`

| Method | Endpoint | Auth | Mô tả |
|--------|----------|------|-------|
| `GET` | `/api/admin/categories` | ✅ Admin | Danh sách danh mục |
| `POST` | `/api/admin/categories` | ✅ Admin | Tạo danh mục |
| `PUT` | `/api/admin/categories/{id}` | ✅ Admin | Cập nhật danh mục |
| `DELETE` | `/api/admin/categories/{id}` | ✅ Admin | Xóa danh mục |

---

## 🔄 SignalR Hub — `/hubs/chat`

### Kết nối
- Yêu cầu JWT token (Authorization header hoặc access_token query)
- Mỗi user được thêm vào group `user_{userId}`
- Tracking kết nối bằng `ConcurrentDictionary`

### Client → Server Methods

| Method | Parameters | Mô tả |
|--------|-----------|-------|
| `SendMessage` | `receiverId: int, content: string` | Gửi tin nhắn |
| `MarkAsRead` | `messageId: int` | Đánh dấu đã đọc |
| `StartTyping` | `receiverId: int` | Thông báo đang gõ |
| `StopTyping` | `receiverId: int` | Thông báo ngừng gõ |

### Server → Client Events

| Event | Data | Mô tả |
|-------|------|-------|
| `ReceiveMessage` | `MessageResponseDto` | Nhận tin nhắn mới |
| `MessageSent` | `MessageResponseDto` | Xác nhận gửi thành công |
| `MessageRead` | `{ messageId, readBy, readAt }` | Tin nhắn đã được đọc |
| `UserTyping` | `{ userId, username }` | User đang gõ |
| `UserStoppedTyping` | `{ userId }` | User ngừng gõ |
| `Error` | `string` | Thông báo lỗi |

---

## ⚙️ Services Architecture

Tất cả services sử dụng **Dependency Injection** qua interface:

### IAuthService → AuthService
- `RegisterAsync(RegisterDto)` → Validate unique → BCrypt hash → Tạo user → Generate JWT
- `LoginAsync(LoginDto)` → Tìm theo Email → Verify BCrypt → Generate JWT

### IJwtService → JwtService
- `GenerateToken(User)` → JWT (24h, HMAC-SHA256)
- `GenerateRefreshToken()` → GUID

### IPasswordResetService → PasswordResetService
- `RequestPasswordResetAsync(email)` → Generate OTP → BCrypt hash → Lưu DB → Gửi email
- `VerifyOtpAsync(email, otpCode)` → Kiểm tra OTP (chưa dùng, chưa hết hạn)
- `ResetPasswordAsync(email, otpCode, newPassword)` → Verify OTP → BCrypt hash password mới

### IEmailService → EmailService
- `SendOtpEmailAsync(toEmail, otpCode)` → Gửi email HTML qua SMTP (MailKit)

### IQuestionService → QuestionService
- CRUD câu hỏi với search, filter, pagination
- Tự động tăng ViewCount khi xem chi tiết
- Xóa cascade: votes + answers + tags + file

### IAnswerService → AnswerService
- CRUD câu trả lời
- Accept: Bỏ accept các câu khác → Accept câu được chọn

### ICategoryService → CategoryService
- CRUD danh mục (Seeding, Create, Update, Delete)
- Đếm số lượng câu hỏi thuộc danh mục

### ITagService → TagService
- Quản lý Tag (Popular, Trending, Suggest)
- Lọc câu hỏi theo Tags với logic AND/OR
- Phân trang Cursor cho câu hỏi theo Tag

### IVoteService → VoteService
- Vote/Update vote cho Question hoặc Answer
- Mỗi user chỉ 1 vote per Question/Answer

### IMessageService → MessageService
- CRUD tin nhắn
- Conversation list: Tìm partners → Lấy last message + unread count → Sắp xếp

### IUserProfileService → UserProfileService
- Get/Update profile (Username unique check)
- Change password (verify old password)
- Upload avatar (validate ext + size, delete old)
- Search users (exclude self)

### IFileUploadService → FileUploadService
- Upload: Validate → Create directory (year/month) → Save with GUID name
- Delete: Handle URL/path → Security check (within wwwroot) → Delete
- GetUrl: Combine base URL + relative path

---

## ⚙️ Configuration (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=UniKnowledge;..."
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "UniKnowledge",
    "Audience": "UniKnowledgeUsers"
  },
  "EmailSettings": {
    "SenderName": "UniKnowledge",
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "",       // Cấu hình qua .env
    "SenderPassword": ""     // Cấu hình qua .env
  },
  "PasswordResetSettings": {
    "OtpExpirationMinutes": 5,
    "OtpLength": 6
  }
}
```

---

## 🔐 Middleware Pipeline

1. **Static Files** — Serve `wwwroot/` (uploads)
2. **CORS** — `AllowAll` policy
3. **Authentication** — JWT Bearer
4. **Authorization** — Role-based (Student, Admin)
5. **Controllers** — Map API controllers
6. **SignalR** — Map `/hubs/chat`

---

## 📊 Tổng Kết

| Hạng mục | Số lượng |
|----------|---------|
| Controllers | 10 |
| Services | 10 |
| Entity Models | 9 |
| DTO folders | 8 |
| API Endpoints | ~40+ |
| SignalR Methods | 4 client→server + 6 server→client |
| NuGet Packages | 7 |
