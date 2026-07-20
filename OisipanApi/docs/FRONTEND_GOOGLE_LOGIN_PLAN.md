# Kế hoạch Frontend - Đăng nhập bằng Google (OisipanMvc)

## Tổng quan
Tài liệu mô tả việc triển khai đăng nhập Google cho ứng dụng `OisipanMvc` (ASP.NET Core MVC), kết nối với Backend API `OisipanApi` đã có endpoint `POST /api/auth/google-login`.

## Kiến trúc thực tế
- HttpClient đã đăng ký tên `"OisipanApi"` trong `Program.cs`, base address lấy từ config `ApiBaseUrl`.
- Cookie Authentication scheme tên `"OisipanCookie"`.
- `AccountController` đã có sẵn `Login`, `Register`, `Logout`, `ForgotPassword` và helper `SignIn(AuthResponse, rememberMe)`.
- `Models/AuthResponse.cs` (Mvc) hiện có UserId, FullName, Email, Role — cần bổ sung `Token`, `AuthProvider` để khớp với Backend.

## Luồng hoạt động
```
User click "Đăng nhập bằng Google" trên Login.cshtml
  -> Google Identity Services SDK mở popup chọn tài khoản
  -> Google trả về ID Token (credential) cho callback JS
  -> JS POST ID Token tới MVC action AccountController.GoogleLoginCallback (không gọi thẳng API để tránh lộ CORS/Client và tận dụng antiforgery + Cookie auth có sẵn)
  -> Action gọi HttpClient "OisipanApi" -> POST api/auth/google-login { idToken }
  -> Backend verify, trả AuthResponse (đã có Token JWT)
  -> Action dùng SignIn() hiện có để tạo Cookie Authentication
  -> Trả JSON { success, redirectUrl } cho JS để redirect
```

## Danh sách thay đổi

### Files mới
- `OisipanMvc/wwwroot/js/google-login.js`
- `OisipanMvc/wwwroot/css/google-login.css`

### Files chỉnh sửa
- `OisipanMvc/Models/AuthResponse.cs` – thêm Token, AuthProvider
- `OisipanMvc/Controllers/AccountController.cs` – thêm action `GoogleLoginCallback`, cập nhật `Login()` GET để truyền ViewBag GoogleClientId
- `OisipanMvc/Views/Account/Login.cshtml` – thêm nút Google + script
- `OisipanMvc/appsettings.json` – thêm `GoogleAuth:ClientId`
- `OisipanMvc/Program.cs` – không cần đổi vì Cookie Auth đã cấu hình sẵn

## Google Cloud Console
Cần thêm Authorized JavaScript origin cho domain chạy OisipanMvc (vd `https://localhost:xxxx`) trong OAuth Client hiện có (`675297933596-....apps.googleusercontent.com`). Không cần redirect URI vì dùng luồng Google Identity Services (One Tap / Sign In With Google button), không dùng redirect-based OAuth.

## Testing Checklist
- [ ] Nút Google hiển thị dưới form login, có divider "HOẶC"
- [ ] Click chọn tài khoản Google -> load spinner -> redirect trang chủ
- [ ] Cookie `OisipanCookie` được set, `User.Identity.IsAuthenticated == true`
- [ ] Tên hiển thị trên nav đúng với tài khoản Google
- [ ] Đăng nhập Google lần 2 với cùng tài khoản -> không tạo account trùng
- [ ] Local login/register vẫn hoạt động bình thường
- [ ] Lỗi token không hợp lệ hiển thị alert rõ ràng, không crash trang

## Bảo mật
- ID Token chỉ đi từ browser -> MVC server -> API backend (không lưu Client Secret ở frontend, không cần dùng Secret trong flow này).
- Endpoint `GoogleLoginCallback` có `[ValidateAntiForgeryToken]` giống các action POST khác.
- Không lưu JWT ở localStorage; JWT chỉ dùng nội bộ nếu cần gọi API kèm Bearer token sau này — hiện tại việc xác thực người dùng ở MVC dựa vào Cookie như cơ chế cũ.
