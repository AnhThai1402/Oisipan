using System;
using System.Data.SqlClient;
class Program {
    static void Main() {
        string connStr = "Server=localhost;Database=Oisipan;Trusted_Connection=True;Encrypt=False";
        using (SqlConnection conn = new SqlConnection(connStr)) {
            conn.Open();
            // 1. Get UserId
            SqlCommand cmd = new SqlCommand("SELECT UserId FROM Accounts WHERE Email='admin123@gmail.com'", conn);
            object userIdObj = cmd.ExecuteScalar();
            if (userIdObj == null) { Console.WriteLine("User not found"); return; }
            Guid userId = (Guid)userIdObj;

            // 2. Get an active VoucherId
            cmd = new SqlCommand("SELECT TOP 1 VoucherId FROM Vouchers WHERE Status=1 AND ExpiryDate > GETDATE()", conn);
            object voucherIdObj = cmd.ExecuteScalar();
            if (voucherIdObj == null) { Console.WriteLine("No active vouchers found"); return; }
            Guid voucherId = (Guid)voucherIdObj;

            // 3. Insert into UserVouchers if not exists
            cmd = new SqlCommand("IF NOT EXISTS(SELECT 1 FROM UserVouchers WHERE UserId=@U AND VoucherId=@V) INSERT INTO UserVouchers(UserId, VoucherId, IsUsed, Status, CreatedAt) VALUES(@U, @V, 0, 1, GETDATE())", conn);
            cmd.Parameters.AddWithValue("@U", userId);
            cmd.Parameters.AddWithValue("@V", voucherId);
            int rows = cmd.ExecuteNonQuery();
            Console.WriteLine("Inserted: " + rows);
        }
    }
}
