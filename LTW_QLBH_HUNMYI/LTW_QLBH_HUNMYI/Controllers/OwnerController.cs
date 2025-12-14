using LTW_QLBH_HUNMYI.Filters;
using LTW_QLBH_HUNMYI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Web;
using System.Web.Mvc;

namespace LTW_QLBH_HUNMYI.Controllers
{
    [CustomAuthorize(AllowedRoles = new[] { "Chủ shop" })]
    public class OwnerController : Controller
    {
        private QLBH_HUNMYI_LTWEntities db = new QLBH_HUNMYI_LTWEntities();

        // GET: Owner - Dashboard
        #region TRANG CHỦ *****ĐÃ XONG*****
        public ActionResult Index()
        {
            ViewBag.Title = "Dashboard";

            // Thống kê tổng quan
            ViewBag.TotalProducts = db.SANPHAM.Count(s => s.TRANGTHAI == "Đang bán");
            ViewBag.TotalOrders = db.HOADON_BAN.Count();
            ViewBag.TotalCustomers = db.KHACHHANG.Count();
            ViewBag.TotalRevenue = db.HOADON_BAN
                .Where(h => h.TRANGTHAI == "Hoàn thành")
                .Sum(h => (decimal?)h.TONGTIEN) ?? 0;

            // Đơn hàng chờ xử lý
            ViewBag.PendingOrders = db.HOADON_BAN.Count(h => h.TRANGTHAI == "Chờ xử lý");

            // Sản phẩm sắp hết hàng
            ViewBag.LowStockProducts = db.SANPHAM.Count(s => s.SOLUONGTON < 10 && s.TRANGTHAI == "Đang bán");

            // Đơn hàng gần đây
            var recentOrders = db.HOADON_BAN
                .OrderByDescending(h => h.NGAYLAP)
                .Take(5)
                .ToList();
            ViewBag.RecentOrders = recentOrders;

            return View();
        }
        // GET: Owner/Profile - Thông tin tài khoản
        public ActionResult Profile()
        {
            ViewBag.Title = "Thông tin tài khoản";

            try
            {
                string staffId = Session["StaffID"]?.ToString();
                
                if (string.IsNullOrEmpty(staffId))
                {
                    TempData["Error"] = "Vui lòng đăng nhập lại!";
                    return RedirectToAction("Login", "Account");
                }

                var staff = db.NHANVIEN.Find(staffId);

                if (staff == null)
                {
                    TempData["Error"] = "Không tìm thấy thông tin tài khoản!";
                    return RedirectToAction("Index");
                }

                // Thống kê cho Owner
                ViewBag.TotalOrders = db.HOADON_BAN.Count();
                ViewBag.TotalRevenue = db.HOADON_BAN
                    .Where(h => h.TRANGTHAI == "Hoàn thành")
                    .Sum(h => (decimal?)h.TONGTIEN) ?? 0;
                ViewBag.TotalProducts = db.SANPHAM.Count(s => s.TRANGTHAI == "Đang bán");

                return View(staff);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra: " + ex.Message;
                return View();
            }
        }

        // POST: Owner/UpdateProfile - Cập nhật thông tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(NHANVIEN model)
        {
            try
            {
                string staffId = Session["StaffID"]?.ToString();
                
                if (string.IsNullOrEmpty(staffId))
                {
                    TempData["Error"] = "Phiên đăng nhập đã hết hạn!";
                    return RedirectToAction("Login", "Account");
                }

                var staff = db.NHANVIEN.Find(staffId);

                if (staff != null)
                {
                    // Cập nhật các trường thông tin
                    staff.HOTENNV = model.HOTENNV;
                    staff.GIOITINH = model.GIOITINH;
                    staff.NGAYSINH = model.NGAYSINH;
                    staff.NGAYVAOLAM = model.NGAYVAOLAM;
                    staff.SDT = model.SDT;
                    staff.EMAIL = model.EMAIL;
                    staff.DIACHI = model.DIACHI;

                    db.SaveChanges();

                    // Cập nhật session
                    Session["StaffName"] = staff.HOTENNV;

                    TempData["Success"] = "Cập nhật thông tin thành công!";
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy thông tin tài khoản!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return RedirectToAction("Profile");
        }

        // GET: Owner/ChangePassword - Đổi mật khẩu
        public ActionResult ChangePassword()
        {
            ViewBag.Title = "Đổi mật khẩu";
            return View();
        }

        // POST: Owner/ChangePassword - Đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            try
            {
                // Kiểm tra mật khẩu mới khớp nhau
                if (newPassword != confirmPassword)
                {
                    TempData["Error"] = "Mật khẩu mới không khớp!";
                    return View();
                }

                // Kiểm tra độ dài mật khẩu
                if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
                {
                    TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự!";
                    return View();
                }

                string userId = Session["UserID"]?.ToString();
                
                if (string.IsNullOrEmpty(userId))
                {
                    TempData["Error"] = "Phiên đăng nhập đã hết hạn!";
                    return RedirectToAction("Login", "Account");
                }

                var account = db.ACCOUNT.Find(userId);

                if (account != null)
                {
                    string oldPasswordHash = GetMD5Hash(oldPassword);

                    // Kiểm tra mật khẩu cũ
                    if (account.PASSWORDHASH != oldPasswordHash)
                    {
                        TempData["Error"] = "Mật khẩu cũ không đúng!";
                        return View();
                    }

                    // Cập nhật mật khẩu mới
                    account.PASSWORDHASH = GetMD5Hash(newPassword);
                    db.SaveChanges();

                    TempData["Success"] = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Profile");
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy tài khoản!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            return View();
        }
        #endregion

        #region SẢN PHẨM *****ĐÃ XONG*****
        // GET: Owner/Products - Quản lý sản phẩm
        public ActionResult Products(string madm)
        {
            ViewBag.Title = "Quản lý sản phẩm";

            // Lấy danh mục cho dropdown
            ViewBag.DanhMucList = new SelectList(db.DANHMUC.ToList(), "MADM", "TENDM");

            // Query sản phẩm kèm navigation property
            var products = db.SANPHAM.Include("DANHMUC");

            // Lọc nếu có chọn danh mục
            if (!string.IsNullOrEmpty(madm))
                products = (System.Data.Entity.Infrastructure.DbQuery<SANPHAM>)products.Where(p => p.MADM == madm);

            return View(products.ToList());
        }

        // GET: Owner/CreateProduct
        public ActionResult CreateProduct()
        {
            ViewBag.Title = "Thêm sản phẩm mới";
            ViewBag.Categories = new SelectList(db.DANHMUC.Where(d => d.TRANGTHAI == "Hiển thị"), "MADM", "TENDM");
            return View();
        }

        // POST: Owner/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProduct(SANPHAM product, HttpPostedFileBase imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Tạo mã sản phẩm mới
                    product.MASP = GenerateNewCode("SP", db.SANPHAM.Select(s => s.MASP).ToList());
                    product.NGAYTAO = DateTime.Now;
                    product.TRANGTHAI = "Đang bán";

                    // Xử lý upload hình ảnh (nếu có)
                    if (imageFile != null && imageFile.ContentLength > 0)
                    {
                        string fileName = System.IO.Path.GetFileName(imageFile.FileName);
                        string path = System.IO.Path.Combine(Server.MapPath("~/Content/Images/Products/"), fileName);
                        imageFile.SaveAs(path);
                        product.HINHANH = fileName;
                    }

                    db.SANPHAM.Add(product);
                    db.SaveChanges();

                    TempData["Success"] = "Thêm sản phẩm thành công!";
                    return RedirectToAction("Products");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            ViewBag.Categories = new SelectList(db.DANHMUC.Where(d => d.TRANGTHAI == "Hiển thị"), "MADM", "TENDM");
            return View(product);
        }

        // GET: Owner/EditProduct/{id}
        public ActionResult EditProduct(string id)
        {
            ViewBag.Title = "Chỉnh sửa sản phẩm";
            var product = db.SANPHAM.Find(id);
            if (product == null)
            {
                return HttpNotFound();
            }
            ViewBag.Categories = new SelectList(db.DANHMUC.Where(d => d.TRANGTHAI == "Hiển thị"), "MADM", "TENDM", product.MADM);
            return View(product);
        }

        // POST: Owner/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduct(SANPHAM product, HttpPostedFileBase imageFile)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = db.SANPHAM.Find(product.MASP);
                    if (existingProduct != null)
                    {
                        existingProduct.TENSP = product.TENSP;
                        existingProduct.MOTA = product.MOTA;
                        existingProduct.GIA = product.GIA;
                        existingProduct.SOLUONGTON = product.SOLUONGTON;
                        existingProduct.MADM = product.MADM;
                        existingProduct.TRANGTHAI = product.TRANGTHAI;

                        // Xử lý upload hình ảnh mới (nếu có)
                        if (imageFile != null && imageFile.ContentLength > 0)
                        {
                            string fileName = System.IO.Path.GetFileName(imageFile.FileName);
                            string path = System.IO.Path.Combine(Server.MapPath("~/Content/Images/Products/"), fileName);
                            imageFile.SaveAs(path);
                            existingProduct.HINHANH = fileName;
                        }

                        db.SaveChanges();
                        TempData["Success"] = "Cập nhật sản phẩm thành công!";
                        return RedirectToAction("Products");
                    }
                }
            }


            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra: " + ex.Message;
            }

            ViewBag.Categories = new SelectList(db.DANHMUC.Where(d => d.TRANGTHAI == "Hiển thị"), "MADM", "TENDM", product.MADM);
            return View(product);
        }

        // GET: Owner/DeleteProduct/{id}
        public ActionResult DeleteProduct(string id)
        {
            if (string.IsNullOrEmpty(id))
                return HttpNotFound();

            var product = db.SANPHAM
                            .Include("DANHMUC")
                            .FirstOrDefault(p => p.MASP == id);

            if (product == null)
                return HttpNotFound();

            return View(product);
        }

        // POST: Owner/DeleteProduct
        [HttpPost, ActionName("DeleteProduct")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDeleteProduct(string id)
        {
            var product = db.SANPHAM.Find(id);
            if (product != null)
            {
                // 🔴 XÓA MỀM
                product.TRANGTHAI = "Ngừng bán";
                db.SaveChanges();

                TempData["Success"] = "Ngừng bán sản phẩm thành công!";
            }

            return RedirectToAction("Products");
        }

        #endregion

        #region DANH MỤC *****ĐÃ XONG*****
        // GET: Owner/Categories
        public ActionResult Categories()
        {
            ViewBag.Title = "Quản lý danh mục";
            var categories = db.DANHMUC.ToList();
            return View(categories);
        }

        // GET: Owner/CreateCategory
        public ActionResult CreateCategory()
        {
            ViewBag.Title = "Thêm danh mục";
            return View();
        }

        // POST: Owner/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCategory(DANHMUC model)
        {
            if (ModelState.IsValid)
            {
                model.MADM = GenerateNewCode("DM", db.DANHMUC.Select(d => d.MADM).ToList());
                model.TRANGTHAI = "Hiển thị";

                db.DANHMUC.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Thêm danh mục thành công!";
                return RedirectToAction("Categories");
            }

            return View(model);
        }

        // GET: Owner/EditCategory/{id}
        public ActionResult EditCategory(string id)
        {
            var category = db.DANHMUC.Find(id);
            if (category == null)
                return HttpNotFound();

            return View(category);
        }

        // POST: Owner/EditCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCategory(DANHMUC model)
        {
            if (ModelState.IsValid)
            {
                var category = db.DANHMUC.Find(model.MADM);
                if (category != null)
                {
                    category.TENDM = model.TENDM;
                    category.TRANGTHAI = model.TRANGTHAI;
                    db.SaveChanges();

                    TempData["Success"] = "Cập nhật danh mục thành công!";
                    return RedirectToAction("Categories");
                }
            }
            return View(model);
        }

        // GET: Owner/DeleteCategory/{id}
        public ActionResult DeleteCategory(string id)
        {
            var category = db.DANHMUC.Find(id);
            if (category == null)
                return HttpNotFound();

            return View(category);
        }

        // POST: Owner/DeleteCategory
        [HttpPost, ActionName("DeleteCategory")]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmDeleteCategory(string id)
        {
            var category = db.DANHMUC.Find(id);

            if (category != null)
            {
                // 🔴 XÓA MỀM
                category.TRANGTHAI = "Ẩn";
                db.SaveChanges();

                TempData["Success"] = "Ẩn danh mục thành công!";
            }

            return RedirectToAction("Categories");
        }

        #endregion

        #region ĐƠN HÀNG *****ĐÃ XONG*****
        // GET: Owner/Orders - Quản lý đơn hàng
        public ActionResult Orders(string status = null, string maHD = null)
        {
            ViewBag.Title = "Quản lý đơn hàng";

            var orders = db.HOADON_BAN.Include("KHACHHANG").Include("NHANVIEN").AsQueryable();

            //Tìm theo mã
            if (!string.IsNullOrEmpty(maHD))
            {
                orders = orders.Where(o => o.MAHD_BAN == maHD);
            }
            //Lọc theo trạng thái
            else if (!string.IsNullOrEmpty(status))
            {
                orders = orders.Where(o => o.TRANGTHAI == status);
            }

            var orderList = orders.OrderByDescending(o => o.NGAYLAP).ToList();
            return View(orderList);
        }

        // GET: Owner/OrderDetails/{id}
        public ActionResult OrderDetails(string id)
        {
            ViewBag.Title = "Chi tiết đơn hàng";

            var order = db.HOADON_BAN.Find(id);
            var details = db.CTHD_BAN.Where(ct => ct.MAHD_BAN == id).ToList();

            ViewBag.Order = order;
            return View(details);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(string id, string status)
        {
            var order = db.HOADON_BAN.Find(id);
            if (order == null)
                return HttpNotFound();

            string currentStatus = order.TRANGTHAI;

            // ❌ Không cho sửa nếu đơn đã kết thúc
            if (currentStatus == "Hoàn thành" || currentStatus == "Đã hủy")
            {
                TempData["Error"] = "Đơn hàng đã kết thúc, không thể thay đổi trạng thái!";
                return RedirectToAction("OrderDetails", new { id });
            }

            // ❌ Không cho quay lui trạng thái
            if (!IsValidNextStatus(currentStatus, status))
            {
                TempData["Error"] = "Chuyển trạng thái không hợp lệ!";
                return RedirectToAction("OrderDetails", new { id });
            }

            // ✅ Cập nhật hợp lệ
            order.TRANGTHAI = status;
            db.SaveChanges();

            TempData["Success"] = "Cập nhật trạng thái thành công!";
            return RedirectToAction("OrderDetails", new { id });
        }
        #endregion

        #region KHÁCH HÀNG *****ĐÃ XONG*****
        // GET: Owner/Customers - Quản lý khách hàng
        public ActionResult Customers(string khach = null)
        {
            ViewBag.Title = "Quản lý khách hàng";
            var customers = db.KHACHHANG.AsQueryable();

            // 🟢 Tìm theo khách hàng (tên hoặc mã)
            if (!string.IsNullOrEmpty(khach))
            {
                customers = customers.Where(c =>
                    c.HOTENKH.Contains(khach) ||
                    c.MAKH.Contains(khach));
            }

            return View(customers.ToList());
        }
        public ActionResult CustomerDetail(string id)
        {
            var kh = db.KHACHHANG.Find(id);
            return View(kh);
        }
        #endregion

        #region NHÂN VIÊN *****ĐÃ XONG*****
        // GET: Owner/Staff - Quản lý nhân viên
        public ActionResult Staff()
        {
            ViewBag.Title = "Quản lý nhân viên";
            var staff = db.NHANVIEN.ToList();
            return View(staff);
        }

        public ActionResult StaffDetail(string id)
        {
            var kh = db.NHANVIEN.Find(id);
            return View(kh);
        }

        // =============================
        // 3. THÊM NHÂN VIÊN (GET)
        // =============================
        public ActionResult CreateStaff()
        {
            NHANVIEN nv = new NHANVIEN();
            nv.MANV = GenerateNewCode("NV", db.NHANVIEN.Select(s => s.MANV).ToList());//tạo mã random
            return View(nv);
        }

        // =============================
        // 3. THÊM NHÂN VIÊN (POST)
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStaff(NHANVIEN nv)
        {
            if (ModelState.IsValid)
            {
                db.NHANVIEN.Add(nv);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(nv);
        }

        // =============================
        // 4. SỬA NHÂN VIÊN (GET)
        // =============================
        public ActionResult EditStaff(string id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var nv = db.NHANVIEN.Find(id);
            if (nv == null) return HttpNotFound();
            return View(nv);
        }

        // =============================
        // 4. SỬA NHÂN VIÊN (POST)
        // =============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStaff(NHANVIEN nv)
        {
            if (ModelState.IsValid)
            {
                db.Entry(nv).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(nv);
        }

        // =============================
        // 5. XÓA (GET)
        // =============================
        public ActionResult DeleteStaff(string id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var nv = db.NHANVIEN.Find(id);
            if (nv == null) return HttpNotFound();
            return View(nv);
        }

        // =============================
        // 5. XÓA (POST)
        // =============================
        [HttpPost, ActionName("DeleteStaff")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(string id)
        {
            var nv = db.NHANVIEN.Find(id);
            if (nv != null)
            {
                db.NHANVIEN.Remove(nv);
                db.SaveChanges();
            }
            return RedirectToAction("Staff");
        }
        #endregion

        #region XƯỞNG IN *****ĐÃ XONG*****
        // GET: Owner/Suppliers - Quản lý xưởng in
        public ActionResult Suppliers()
        {
            ViewBag.Title = "Quản lý xưởng in";
            var suppliers = db.XUONGIN.ToList();
            return View(suppliers);
        }

        public ActionResult CreateSupplier()
        {
            XUONGIN xi = new XUONGIN();
            xi.MAXI = GenerateNewCode("XI", db.XUONGIN.Select(s => s.MAXI).ToList());//tạo mã random
            return View(xi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSupplier(XUONGIN model)
        {
            if (ModelState.IsValid)
            {
                // kiểm tra trùng mã
                var check = db.XUONGIN.Find(model.MAXI);
                if (check != null)
                {
                    ModelState.AddModelError("", "Mã xưởng đã tồn tại");
                    return View(model);
                }

                // mặc định trạng thái
                model.TRANGTHAI = "Hoạt động";

                db.XUONGIN.Add(model);
                db.SaveChanges();

                return RedirectToAction("Suppliers");
            }

            return View(model);
        }

        public ActionResult EditSupplier(string id)
        {
            if (id == null) return HttpNotFound();

            var xi = db.XUONGIN.Find(id);
            if (xi == null) return HttpNotFound();

            return View(xi);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSupplier(XUONGIN model)
        {
            if (ModelState.IsValid)
            {
                var xi = db.XUONGIN.Find(model.MAXI);
                if (xi == null) return HttpNotFound();

                xi.TENXI = model.TENXI;
                xi.DIACHI = model.DIACHI;
                xi.SDT = model.SDT;
                xi.EMAIL = model.EMAIL;
                xi.NGUOILIENHE = model.NGUOILIENHE;
                xi.TRANGTHAI = model.TRANGTHAI;

                db.SaveChanges();
                return RedirectToAction("Suppliers");
            }
            return View(model);
        }

        public ActionResult DeleteSupplier(string id)
        {
            if (id == null) return HttpNotFound();

            var xi = db.XUONGIN.Find(id);
            if (xi != null)
            {
                xi.TRANGTHAI = "Ngừng hợp tác";
                db.SaveChanges();
            }

            return RedirectToAction("Suppliers");
        }
        #endregion

        #region PHIẾU NHẬP *****ĐÃ XONG*****
        //GET: Owner/ImportReceipts - Quản lý phiếu nhập
        // ======= IMPORT RECEIPTS ========
        public ActionResult ImportReceipts()
        {
            ViewBag.Title = "Phiếu nhập hàng";
            var imports = db.PHIEUNHAP.ToList();
            return View(imports);
        }

        // GET: Owner/CreateImport
        public ActionResult CreateImport()
        {
            ViewBag.XuongIn = db.XUONGIN.ToList();           // nhà cung cấp
            ViewBag.DanhMucList = db.DANHMUC.ToList();       // danh mục sản phẩm
            ViewBag.Products = db.SANPHAM.ToList();          // tất cả sản phẩm
            return View();
        }

        // POST: Owner/CreateImport
        [HttpPost]
        public ActionResult CreateImport(PHIEUNHAP model, string[] productId, int[] qty, decimal[] price)
        {
            model.MAPN = "PN" + new Random().Next(1000, 9999);
            model.MANV = Session["StaffID"].ToString();
            model.NGAYNHAP = DateTime.Now;

            db.PHIEUNHAP.Add(model);

            decimal tongTien = 0;

            for (int i = 0; i < productId.Length; i++)
            {
                var ct = new CHITIETPHIEUNHAP
                {
                    MAPN = model.MAPN,
                    MASP = productId[i],
                    SOLUONG = qty[i],
                    DONGIA = price[i]
                };
                db.CHITIETPHIEUNHAP.Add(ct);

                var sp = db.SANPHAM.Find(productId[i]);
                if (sp != null)
                    sp.SOLUONGTON += qty[i];

                tongTien += qty[i] * price[i];
            }

            model.TONGTIEN = tongTien;

            db.SaveChanges();

            return RedirectToAction("ImportReceipts");
        }
        #endregion

        #region BÁO CÁO *****ĐÃ XONG*****
        // GET: Owner/Reports - Báo cáo
        public ActionResult Reports()
        {
            ViewBag.Title = "Báo cáo thống kê";

            //// Doanh thu theo tháng
            //var monthlyRevenue = db.HOADON_BAN
            //    .Where(h => h.TRANGTHAI == "Hoàn thành")
            //    .GroupBy(h => new { h.NGAYLAP.Value.Year, h.NGAYLAP.Value.Month })
            //    .Select(g => new
            //    {
            //        Year = g.Key.Year,
            //        Month = g.Key.Month,
            //        Revenue = g.Sum(h => h.TONGTIEN)
            //    })
            //    .OrderByDescending(x => x.Year)
            //    .ThenByDescending(x => x.Month)
            //    .Take(12)
            //    .ToList();

            //ViewBag.MonthlyRevenue = monthlyRevenue;

            return View();
        }

        [HttpGet]
        public JsonResult GetRevenue(string type, string fromDate, string toDate)
        {
            DateTime from, to;

            // Xử lý theo type
            if (type == "day") // yyyy-MM-dd
            {
                if (!DateTime.TryParse(fromDate, out from)) from = DateTime.Today;
                if (!DateTime.TryParse(toDate, out to)) to = DateTime.Today;

                from = from.Date;
                to = to.Date.AddDays(1).AddTicks(-1);
            }
            else if (type == "month") // yyyy-MM
            {
                try
                {
                    var fromParts = fromDate.Split('-'); // ["2025", "01"]
                    var toParts = toDate.Split('-');

                    from = new DateTime(int.Parse(fromParts[0]), int.Parse(fromParts[1]), 1);
                    to = new DateTime(int.Parse(toParts[0]), int.Parse(toParts[1]), 1)
                             .AddMonths(1).AddTicks(-1);
                }
                catch
                {
                    var today = DateTime.Today;
                    from = new DateTime(today.Year, today.Month, 1);
                    to = from.AddMonths(1).AddTicks(-1);
                }
            }
            else if (type == "year") // yyyy
            {
                try
                {
                    int fromYear = int.Parse(fromDate);
                    int toYear = int.Parse(toDate);
                    from = new DateTime(fromYear, 1, 1);
                    to = new DateTime(toYear, 12, 31, 23, 59, 59, 999);
                }
                catch
                {
                    int year = DateTime.Today.Year;
                    from = new DateTime(year, 1, 1);
                    to = new DateTime(year, 12, 31, 23, 59, 59, 999);
                }
            }
            else
            {
                from = DateTime.Today;
                to = DateTime.Today.AddDays(1).AddTicks(-1);
            }

            // Lấy dữ liệu
            var query = db.HOADON_BAN
                .Where(h => h.TRANGTHAI == "Hoàn thành"
                            && h.NGAYLAP.HasValue
                            && h.NGAYLAP.Value >= from
                            && h.NGAYLAP.Value <= to)
                .ToList();

            var result = new List<RevenueDto>();

            if (query.Any())
            {
                if (type == "day")
                {
                    result = query
                        .GroupBy(h => h.NGAYLAP.Value.Date)
                        .Select(g => new RevenueDto
                        {
                            Label = g.Key.ToString("yyyy-MM-dd"),
                            Total = g.Sum(h => h.TONGTIEN ?? 0)
                        })
                        .OrderBy(r => r.Label)
                        .ToList();
                }
                else if (type == "month")
                {
                    result = query
                        .GroupBy(h => new { h.NGAYLAP.Value.Year, h.NGAYLAP.Value.Month })
                        .Select(g => new RevenueDto
                        {
                            Label = g.Key.Year + "-" + g.Key.Month.ToString("D2"),
                            Total = g.Sum(h => h.TONGTIEN ?? 0)
                        })
                        .OrderBy(r => r.Label)
                        .ToList();
                }
                else if (type == "year")
                {
                    result = query
                        .GroupBy(h => h.NGAYLAP.Value.Year)
                        .Select(g => new RevenueDto
                        {
                            Label = g.Key.ToString(),
                            Total = g.Sum(h => h.TONGTIEN ?? 0)
                        })
                        .OrderBy(r => r.Label)
                        .ToList();
                }
            }

            return Json(new
            {
                labels = result.Select(r => r.Label).ToArray(),
                data = result.Select(r => r.Total).ToArray()
            }, JsonRequestBehavior.AllowGet);
        }



        [HttpGet]
        public JsonResult GetTopProducts(string type, string fromDate, string toDate)
        {
            DateTime from, to;

            if (type == "day") // yyyy-MM-dd
            {
                if (!DateTime.TryParse(fromDate, out from)) from = DateTime.Today;
                if (!DateTime.TryParse(toDate, out to)) to = DateTime.Today;

                from = from.Date;
                to = to.Date.AddDays(1).AddTicks(-1);
            }
            else if (type == "month") // yyyy-MM
            {
                try
                {
                    var fromParts = fromDate.Split('-'); // ["2025", "01"]
                    var toParts = toDate.Split('-');

                    from = new DateTime(int.Parse(fromParts[0]), int.Parse(fromParts[1]), 1);
                    to = new DateTime(int.Parse(toParts[0]), int.Parse(toParts[1]), 1)
                             .AddMonths(1).AddTicks(-1);
                }
                catch
                {
                    var today = DateTime.Today;
                    from = new DateTime(today.Year, today.Month, 1);
                    to = from.AddMonths(1).AddTicks(-1);
                }
            }
            else if (type == "year") // yyyy
            {
                try
                {
                    int fromYear = int.Parse(fromDate);
                    int toYear = int.Parse(toDate);
                    from = new DateTime(fromYear, 1, 1);
                    to = new DateTime(toYear, 12, 31, 23, 59, 59, 999);
                }
                catch
                {
                    int year = DateTime.Today.Year;
                    from = new DateTime(year, 1, 1);
                    to = new DateTime(year, 12, 31, 23, 59, 59, 999);
                }
            }
            else
            {
                from = DateTime.Today;
                to = DateTime.Today.AddDays(1).AddTicks(-1);
            }

            var topProducts = db.CTHD_BAN
                .Where(c => c.HOADON_BAN.NGAYLAP.HasValue
                            && c.HOADON_BAN.NGAYLAP.Value >= from
                            && c.HOADON_BAN.NGAYLAP.Value <= to)
                .ToList()
                .GroupBy(c => new { c.MASP, c.SANPHAM.TENSP })
                .Select(g => new
                {
                    Name = g.Key.TENSP,
                    Quantity = g.Sum(c => c.SOLUONG ?? 0)
                })
                .OrderByDescending(x => x.Quantity)
                .Take(10)
                .ToList();

            return Json(new
            {
                labels = topProducts.Select(p => p.Name).ToArray(),
                data = topProducts.Select(p => p.Quantity).ToArray()
            }, JsonRequestBehavior.AllowGet);
        }


        #endregion

        #region HELPER METHODS

        // Tạo mã tự động
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
            return prefix + (maxNumber + 1).ToString();
        }

        // Mã hóa MD5
        private string GetMD5Hash(string input)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        // Kiếm tra luồng tiến độ xử lý đơn hàng
        private bool IsValidNextStatus(string current, string next)
        {
            var flow = new Dictionary<string, List<string>>
    {
        { "Chờ xử lý", new List<string> { "Đang xử lý", "Đã hủy" } },
        { "Đang xử lý", new List<string> { "Hoàn thành", "Đã hủy" } }
    };

            return flow.ContainsKey(current) && flow[current].Contains(next);
        }

        #endregion

        #region DISPOSE
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion
    }
}