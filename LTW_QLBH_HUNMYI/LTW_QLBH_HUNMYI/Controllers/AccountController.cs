using LTW_QLBH_HUNMYI.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace LTW_QLBH_HUNMYI.Controllers
{
    public class AccountController : Controller
    {
        QLBH1Entities db = new QLBH1Entities();
        // GET: Account/Login
        public ActionResult Login()
        {
            if (Session["UserID"] != null)
            {
                return RedirectToHome();
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin!";
                return View();
            }

            string passwordHash = GetMD5Hash(password);

            // Kiểm tra username và password trước (không quan tâm status)
            var account = db.ACCOUNT.FirstOrDefault(a =>
                a.USERNAME == username &&
                a.PASSWORDHASH == passwordHash);

            if (account != null)
            {
                // Kiểm tra trạng thái tài khoản
                if (account.TRANGTHAI == "Khóa")
                {
                    ViewBag.Error = "Tài khoản của bạn đã bị khóa! Vui lòng liên hệ quản trị viên.";
                    return View();
                }
                else if (account.TRANGTHAI != "Hoạt động")
                {
                    ViewBag.Error = "Tài khoản không ở trạng thái hoạt động!";
                    return View();
                }

                // Lưu thông tin vào session
                Session["UserID"] = account.USERID;
                Session["Username"] = account.USERNAME;
                Session["Role"] = account.VAITRO;
                Session["Email"] = account.EMAIL;

                if (account.VAITRO == "Khách")
                {
                    Session["CustomerID"] = account.MAKH;
                    Session["CustomerName"] = db.KHACHHANG.Find(account.MAKH)?.HOTENKH;
                }
                else
                {
                    Session["StaffID"] = account.MANV;
                    var nhanvien = db.NHANVIEN.Find(account.MANV);
                    Session["StaffName"] = nhanvien?.HOTENNV;
                    Session["Position"] = nhanvien?.CHUCVU;
                }

                return RedirectToHome();
            }
            else
            {
                ViewBag.Error = "Tên đăng nhập hoặc mật khẩu không đúng!";
                return View();
            }
        }

        // GET: Account/Register
        public ActionResult Register()
        {
            if (Session["UserID"] != null)
            {
                return RedirectToHome();
            }
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Register(string username, string password, string confirmPassword,
                             string hoTen, string gioiTinh, string sdt, string email, string diaChi)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            // Trước khi gọi Any() hoặc truy vấn DB, kiểm tra DB có sẵn sàng không
            try
            {
                bool canConnect = db.Database.Exists(); // EF6: kiểm tra DB
                if (!canConnect)
                {
                    ViewBag.Error = "Không thể kết nối tới cơ sở dữ liệu. Vui lòng kiểm tra cấu hình kết nối hoặc dịch vụ SQL Server.";
                    return View();
                }
            }
            catch (SqlException sqlEx)
            {
                // Lỗi kết nối/SQL -> hiện thông báo thân thiện, log chi tiết nếu cần
                // (Không show chi tiết lỗi cho user, chỉ log nội bộ)
                ViewBag.Error = "Lỗi kết nối tới cơ sở dữ liệu. Vui lòng liên hệ quản trị hệ thống.";
                // TODO: log sqlEx.Message / sqlEx.Number vào file log
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra khi kiểm tra cơ sở dữ liệu.";
                // TODO: log ex
                return View();
            }

            // Kiểm tra username/email/SDT tồn tại
            if (db.ACCOUNT.Any(a => a.USERNAME == username))
            {
                ViewBag.Error = "Tên đăng nhập đã tồn tại!";
                return View();
            }
            
            // Kiểm tra email trong cả ACCOUNT và KHACHHANG
            if (db.ACCOUNT.Any(a => a.EMAIL == email))
            {
                ViewBag.Error = "Email đã được sử dụng!";
                return View();
            }
            
            if (db.KHACHHANG.Any(k => k.EMAIL == email))
            {
                ViewBag.Error = "Email đã được sử dụng!";
                return View();
            }
            
            // Kiểm tra SDT trong cả ACCOUNT và KHACHHANG
            if (db.ACCOUNT.Any(a => a.SDT == sdt))
            {
                ViewBag.Error = "Số điện thoại đã được sử dụng!";
                return View();
            }
            
            if (db.KHACHHANG.Any(k => k.SDT == sdt))
            {
                ViewBag.Error = "Số điện thoại đã được sử dụng!";
                return View();
            }

            string newMaKH = null;
            
            try
            {
                // Tạo mã KH mới
                newMaKH = GenerateNewCode("KH", db.KHACHHANG.Select(k => k.MAKH).ToList());
                string newAccountID = GenerateNewCode("ACC", db.ACCOUNT.Select(a => a.USERID).ToList());
                string newMaGH = GenerateNewCode("GH", db.GIOHANG.Select(g => g.MAGH).ToList());

                // DISABLE TẤT CẢ TRIGGERS
                db.Database.ExecuteSqlCommand("ALTER TABLE KHACHHANG DISABLE TRIGGER ALL");
                db.Database.ExecuteSqlCommand("ALTER TABLE ACCOUNT DISABLE TRIGGER ALL");
                db.Database.ExecuteSqlCommand("ALTER TABLE GIOHANG DISABLE TRIGGER ALL");

                try
                {
                    // 1. INSERT KHACHHANG TRƯỚC
                    string sqlKH = @"INSERT INTO KHACHHANG (MAKH, HOTENKH, GIOITINH, SDT, EMAIL, DIACHI, NGAYDANGKY) 
                                    VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)";
                    db.Database.ExecuteSqlCommand(sqlKH, 
                        newMaKH, hoTen, gioiTinh, sdt, email, diaChi ?? "", DateTime.Now);

                    // 2. INSERT ACCOUNT (KHACHHANG đã tồn tại)
                    string sqlAccount = @"INSERT INTO ACCOUNT (USERID, USERNAME, PASSWORDHASH, VAITRO, MAKH, MANV, EMAIL, SDT, TRANGTHAI) 
                                         VALUES (@p0, @p1, @p2, @p3, @p4, NULL, @p5, @p6, @p7)";
                    db.Database.ExecuteSqlCommand(sqlAccount,
                        newAccountID, username, GetMD5Hash(password), "Khách", newMaKH, email, sdt, "Hoạt động");

                    // 3. INSERT GIOHANG (KHACHHANG đã tồn tại)
                    string sqlGH = @"INSERT INTO GIOHANG (MAGH, MAKH, NGAYTAO) 
                                    VALUES (@p0, @p1, @p2)";
                    db.Database.ExecuteSqlCommand(sqlGH,
                        newMaGH, newMaKH, DateTime.Now);
                }
                finally
                {
                    // ENABLE LẠI TRIGGERS
                    db.Database.ExecuteSqlCommand("ALTER TABLE KHACHHANG ENABLE TRIGGER ALL");
                    db.Database.ExecuteSqlCommand("ALTER TABLE ACCOUNT ENABLE TRIGGER ALL");
                    db.Database.ExecuteSqlCommand("ALTER TABLE GIOHANG ENABLE TRIGGER ALL");
                }

                TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                // Lỗi validation
                var errorMessages = dbEx.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.PropertyName + ": " + x.ErrorMessage);
                var fullErrorMessage = string.Join("; ", errorMessages);
                
                ViewBag.Error = "Dữ liệu không hợp lệ: " + fullErrorMessage;
                return View();
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException dbUpdateEx)
            {
                // Lỗi database constraint
                var innerMessage = dbUpdateEx.InnerException?.InnerException?.Message ?? dbUpdateEx.Message;
                ViewBag.Error = "Lỗi cơ sở dữ liệu: " + innerMessage;
                return View();
            }
            catch (SqlException sqlEx)
            {
                ViewBag.Error = "Lỗi SQL Server: " + sqlEx.Message;
                return View();
            }
            catch (Exception ex)
            {
                var innerMsg = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                ViewBag.Error = "Lỗi đăng ký: " + innerMsg;
                return View();
            }
        }


        // Đăng xuất
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        // Trang Access Denied
        public ActionResult AccessDenied()
        {
            return View();
        }

        // Helper methods
        private ActionResult RedirectToHome()
        {
            string role = Session["Role"]?.ToString();
            switch (role)
            {
                case "Chủ shop":
                    return RedirectToAction("Index", "Owner");
                case "Nhân viên":
                    return RedirectToAction("Index", "Staff");
                case "Khách":
                    return RedirectToAction("Index", "Customer");
                default:
                    return RedirectToAction("Login");
            }
        }

        private string GetMD5Hash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        private string GenerateNewCode(string prefix, System.Collections.Generic.List<string> existingCodes)
        {
            int maxNumber = 0;
            foreach (var code in existingCodes)
            {
                if (code.StartsWith(prefix))
                {
                    string numberPart = code.Substring(prefix.Length);
                    if (int.TryParse(numberPart, out int number))
                    {
                        if (number > maxNumber)
                            maxNumber = number;
                    }
                }
            }
            return prefix + (maxNumber + 1).ToString("D3");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}