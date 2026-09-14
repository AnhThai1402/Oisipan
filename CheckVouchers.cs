using System;
using System.Data.SqlClient;
class Program {
    static void Main() {
        string connStr = "Server=localhost;Database=DATN_Oishipan;Trusted_Connection=True;Encrypt=False";
        using (SqlConnection conn = new SqlConnection(connStr)) {
            conn.Open();
            using (SqlCommand cmd = new SqlCommand("SELECT a.Email, v.Code, uv.IsUsed, v.EndDate, v.IsActive FROM UserVouchers uv INNER JOIN Accounts a ON uv.UserId = a.UserId INNER JOIN Vouchers v ON uv.VoucherId = v.VoucherId", conn)) {
                using (SqlDataReader reader = cmd.ExecuteReader()) {
                    while (reader.Read()) {
                        Console.WriteLine(string.Format("{0} - {1} - Used:{2} - EndDate:{3} - Active:{4}", reader[0], reader[1], reader[2], reader[3], reader[4]));
                    }
                }
            }
        }
    }
}
