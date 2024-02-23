using FinalDAW2.Data;
using FinalDAW2.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProiectDAW.Controllers
{
    public class GroupsController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public GroupsController(ApplicationDbContext context,
                                UserManager<ApplicationUser> userManager,
                                RoleManager<IdentityRole> roleManager
                                )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }
        public IActionResult Index()
        {
            var groups = from grupuri in db.Groups
                         orderby grupuri.Name
                         select grupuri;
            ViewBag.GroupsList = groups;

            return View();
        }

        public IActionResult New()
        {
            Group group = new Group();
            return View(group);
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult New(Group group)
        {
            group.CreatedDate = DateTime.Now;
            group.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Groups.Add(group);
                //db.ApplicationUserGroups.Add(g)
                db.SaveChanges();

                var currentUserId = _userManager.GetUserId(User);
                var userGroup = new ApplicationUserGroup
                {
                    UserId = currentUserId,
                    GroupId = group.Id 
                };
                db.ApplicationUserGroups.Add(userGroup);
                db.SaveChanges();

                TempData["message"] = "Grupul a fost creat și te-ai alăturat automat!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");

            }
            else
            {
                return View(group);
            }
        }

        [Authorize(Roles = "Admin,User")]
        public IActionResult Show(int id)
        {
            SetAccessRights();
            string userId = _userManager.GetUserId(User); 

            ViewBag.UserId = userId;

            Group group = db.Groups.Include(g => g.Posts)
                                   .Where(g => g.Id == id)
                                   .FirstOrDefault();

            bool isUserInGroup = db.ApplicationUserGroups.Any(ag => ag.GroupId == id && ag.UserId == userId);
            ViewBag.IsUserInGroup = isUserInGroup;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            return View(group);
        }



        [Authorize(Roles = "Admin,User")]
        public ActionResult Delete(int id)
        {
            Group group = db.Groups.Find(id);

            if (group == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && group.UserId != _userManager.GetUserId(User))
            {
                TempData["message"] = "Nu aveți dreptul să ștergeți acest grup.";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            
            Group grup = db.Groups.Include("Posts")
                                         .Where(art => art.Id == id)
                                         .First();
            db.Groups.Remove(grup);
            db.SaveChanges();

            TempData["message"] = "Grupul a fost șters cu succes.";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }


        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("Admin"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.EsteAdmin = User.IsInRole("Admin");

            ViewBag.UserCurent = _userManager.GetUserId(User);
        }

        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult JoinGroup(int groupId)
        {
            var group = db.Groups.Find(groupId);

            if (group == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            var isUserInGroup = db.ApplicationUserGroups
                .Any(ug => ug.UserId == currentUserId && ug.GroupId == groupId);

            if (!isUserInGroup)
            {
                // Adauga utilizatorul în grup
                var userGroup = new ApplicationUserGroup
                {
                    UserId = currentUserId,
                    GroupId = groupId
                };

                db.ApplicationUserGroups.Add(userGroup);
                db.SaveChanges();

                TempData["message"] = "Te-ai alăturat cu succes grupului!";
                TempData["messageType"] = "alert-success";
            }
            else
            {
                TempData["message"] = "Ești deja membru al acestui grup!";
                TempData["messageType"] = "alert-warning";
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        [Authorize(Roles = "User,Admin")]
        public IActionResult LeaveGroup(int groupId)
        {
            var group = db.Groups.Find(groupId);

            if (group == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);

            var isUserInGroup = db.ApplicationUserGroups
                .Any(ug => ug.UserId == currentUserId && ug.GroupId == groupId);

            if (isUserInGroup)
            {
                var userGroup = db.ApplicationUserGroups.FirstOrDefault(ug => ug.UserId == currentUserId && ug.GroupId == groupId);

                if (userGroup != null)
                {
                    db.ApplicationUserGroups.Remove(userGroup);
                    db.SaveChanges();

                    TempData["message"] = "Ai părăsit cu succes grupul!";
                    TempData["messageType"] = "alert-success";
                }
            }
            else
            {
                TempData["message"] = "Nu ești membru al acestui grup!";
                TempData["messageType"] = "alert-warning";
            }

            return RedirectToAction("Index");
        }


    }
}
