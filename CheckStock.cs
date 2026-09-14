using System;
using System.Data.SqlClient;
class Program {
    static void Main() {
        string connStr = "Server=localhost;Database=Oisipan;Trusted_Connection=True;Encrypt=False";
        using (SqlConnection conn = new SqlConnection(connStr)) {
            conn.Open();
            SqlCommand cmd = new SqlCommand("SELECT TOP 10 p.Name, p.StockQuantity as ProductStock, pv.ProductVariantId, pv.StockQuantity as VariantStock FROM Products p JOIN ProductVariants pv ON p.ProductId = pv.ProductId", conn);
            using (SqlDataReader r = cmd.ExecuteReader()) {
                while(r.Read()) {
                    Console.WriteLine(string.Format("{0} | ProdStock: {1} | VarStock: {3}", r[0], r[1], r[2], r[3]));
                }
            }
        }
    }
}
