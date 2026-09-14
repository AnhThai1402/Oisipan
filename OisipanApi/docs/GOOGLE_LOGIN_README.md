# Hướng dẫn Đăng nhập bằng Google

## Tổng quan
API hỗ trợ 2 phương thức authentication:
1. **Local Authentication**: Đăng ký và đăng nhập bằng email/password
2. **Google OAuth 2.0**: Đăng nhập bằng tài khoản Google

## Endpoints

### 1. Đăng ký Local (POST /api/auth/register)
Tạo tài khoản mới với email và password.

**Request Body:**
```json
{
  "fullName": "Nguyen Van A",
  "email": "user@example.com",
  "phoneNumber": "0901234567",
  "password": "Password123!",
  "confirmPassword": "Password123!",
  "address": "123 Street, City"
}
```

**Response:**
```json
{
  "userId": 1,
  "fullName": "Nguyen Van A",
  "email": "user@example.com",
  "role": "User",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "authProvider": "Local"
}
```

### 2. Đăng nhập Local (POST /api/auth/login)
Đăng nhập với email và password đã đăng ký.

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Response:** Giống như Register

### 3. Đăng nhập Google (POST /api/auth/google-login)
Đăng nhập hoặc đăng ký tự động bằng Google ID Token.

**Request Body:**
```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6..."
}
```

**Response:**
```json
{
  "userId": 2,
  "fullName": "John Doe",
  "email": "john.doe@gmail.com",
  "role": "User",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "authProvider": "Google"
}
```

**Luồng hoạt động:**
- Nếu user chưa tồn tại: tự động tạo account mới
- Nếu user đã có account với cùng email: link GoogleId vào account đó
- Nếu user đã login Google trước đó: đăng nhập thành công

### 4. Lấy thông tin User hiện tại (GET /api/auth/me)
Protected endpoint - yêu cầu JWT token trong Authorization header.

**Headers:**
```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

**Response:**
```json
{
  "userId": 1,
  "fullName": "Nguyen Van A",
  "email": "user@example.com",
  "role": "User",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "authProvider": "Local"
}
```

## Cách lấy Google ID Token

### Frontend Web (React/Angular/Vue)
```javascript
// 1. Cài đặt Google Sign-In
npm install @react-oauth/google

// 2. Setup GoogleOAuthProvider
import { GoogleOAuthProvider, GoogleLogin } from '@react-oauth/google';

<GoogleOAuthProvider clientId="YOUR_GOOGLE_CLIENT_ID">
  <GoogleLogin
	onSuccess={(credentialResponse) => {
	  const idToken = credentialResponse.credential;
	  // Gửi idToken đến backend
	  fetch('http://localhost:5188/api/auth/google-login', {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		body: JSON.stringify({ idToken })
	  })
	  .then(res => res.json())
	  .then(data => {
		// Lưu token JWT
		localStorage.setItem('token', data.token);
	  });
	}}
	onError={() => console.log('Login Failed')}
  />
</GoogleOAuthProvider>
```

### Mobile (Flutter)
```dart
// 1. Thêm package
dependencies:
  google_sign_in: ^6.1.0

// 2. Sign in và lấy ID Token
import 'package:google_sign_in/google_sign_in.dart';

final GoogleSignIn _googleSignIn = GoogleSignIn(
  clientId: 'YOUR_GOOGLE_CLIENT_ID',
);

Future<void> signInWithGoogle() async {
  final GoogleSignInAccount? account = await _googleSignIn.signIn();
  final GoogleSignInAuthentication auth = await account!.authentication;
  final String? idToken = auth.idToken;

  // Gửi idToken đến backend
  final response = await http.post(
	Uri.parse('http://localhost:5188/api/auth/google-login'),
	headers: {'Content-Type': 'application/json'},
	body: jsonEncode({'idToken': idToken}),
  );
}
```

## Sử dụng JWT Token

Sau khi đăng nhập thành công, client nhận được JWT token. Token này cần được gửi kèm trong header của các API request tiếp theo:

```http
GET /api/auth/me HTTP/1.1
Host: localhost:5188
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Token có hiệu lực:** 1440 phút (24 giờ)

## Xử lý Lỗi

### 401 Unauthorized
- Token không hợp lệ hoặc đã hết hạn
- Email/password không đúng
- Google ID Token không hợp lệ

### 400 Bad Request
- Tài khoản đã bị khóa
- Validation errors (email format, password length, etc.)
- Đăng nhập sai phương thức (ví dụ: account đăng ký bằng Google nhưng cố login bằng password)

### 404 Not Found
- User không tồn tại (endpoint /me)
- Email không tìm thấy (forgot password)

## Database Schema

### Account Table
| Column | Type | Description |
|--------|------|-------------|
| UserId | int | Primary Key |
| FullName | nvarchar(100) | Tên đầy đủ |
| Email | nvarchar(100) | Email (unique) |
| PhoneNumber | nvarchar(15) | Số điện thoại (nullable, unique) |
| Password | nvarchar(max) | Password hash (nullable cho Google users) |
| GoogleId | nvarchar(100) | Google User ID (nullable, unique) |
| AuthProvider | nvarchar(20) | "Local" hoặc "Google" |
| Role | nvarchar(20) | "User" hoặc "Admin" |
| Address | nvarchar(max) | Địa chỉ (nullable) |
| Status | bit | Active/Inactive |

## Cấu hình

### appsettings.json
```json
{
  "GoogleAuth": {
  "ClientId": "",
  "ClientSecret": ""
  },
  "JwtSettings": {
	"Secret": "OisipanSecretKey2024-ThisIsAVeryLongSecretKeyForJWT-MinimumLength256Bits",
	"Issuer": "OisipanApi",
	"Audience": "OisipanClient",
	"ExpiryMinutes": 1440
  }
}
```

For local development, provide the values outside source control using the
`GoogleAuth__ClientId` and `GoogleAuth__ClientSecret` environment variables.
The JWT secret should likewise be supplied through `JwtSettings__Secret`.

## Testing với Postman

1. **Test Register:**
   - POST http://localhost:5188/api/auth/register
   - Body: JSON với fullName, email, phoneNumber, password, confirmPassword

2. **Test Login:**
   - POST http://localhost:5188/api/auth/login
   - Body: JSON với email, password
   - Copy token từ response

3. **Test Google Login:**
   - Lấy Google ID Token từ Google OAuth Playground: https://developers.google.com/oauthplayground/
   - Chọn scope: https://www.googleapis.com/auth/userinfo.email
   - Exchange authorization code for tokens
   - Copy ID Token
   - POST http://localhost:5188/api/auth/google-login
   - Body: `{ "idToken": "YOUR_ID_TOKEN" }`

4. **Test Protected Endpoint:**
   - GET http://localhost:5188/api/auth/me
   - Headers: `Authorization: Bearer YOUR_JWT_TOKEN`

## Security Notes

⚠️ **Quan trọng:**
- Không commit Google Client Secret vào Git
- Sử dụng Environment Variables hoặc User Secrets trong production
- JWT Secret phải là chuỗi random ít nhất 32 ký tự
- Trong production, nên set ExpiryMinutes ngắn hơn và implement Refresh Token
- Enable HTTPS trong production

## Troubleshooting

### "Google token không hợp lệ"
- Kiểm tra Google ID Token còn hạn chưa (thường 1 giờ)
- Đảm bảo ClientId trong backend config khớp với ClientId trong frontend
- Token phải là ID Token, không phải Access Token

### "Tài khoản này được đăng ký bằng Google"
- User đã đăng ký bằng Google trước đó
- Phải login bằng Google, không thể dùng email/password

### Database Migration Failed
- Ensure SQL Server đang chạy
- Check connection string trong appsettings.json
- Chạy: `dotnet ef database update`

---

**Tác giả:** Oisipan Development Team  
**Version:** 1.0  
**Ngày cập nhật:** 2026-07-20
