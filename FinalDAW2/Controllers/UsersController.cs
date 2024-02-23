using Microsoft.AspNetCore.Mvc;
using FinalDAW2.Models;
using System.Threading.Tasks;
using FinalDAW2.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using NuGet.Protocol.Plugins;

namespace FinalDAW2.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }

        [Route("Useri")]
        public IActionResult Index()
        {
            ViewBag.EsteAdmin = User.IsInRole("Admin");
            ViewBag.UserCurent = _userManager.GetUserId(User);

            var users = from user in db.Users
                        orderby user.UserName
                        select user;

            ViewBag.UsersList = users;

            var search = HttpContext.Request.Query["search"].ToString().Trim();

            if (!string.IsNullOrEmpty(search))
            {
                List<string> usersIds = db.ApplicationUsers
                                        .Where(at => at.UserName.Contains(search))
                                        .Select(a => a.Id)
                                        .ToList();

                users = db.ApplicationUsers
                        .Where(user => usersIds.Contains(user.Id))
                        .OrderBy(a => a.UserName);

                ViewBag.UsersList = users;
                ViewBag.UserList = search;

                ViewBag.PaginationBaseUrl = $"/Users/Index/?search={search}";
            }

            return View();
        }



        public async Task<ActionResult> Show(string id)
        {
            ViewBag.UserNume = _userManager.GetUserName(User);
            ViewBag.EsteAdmin = User.IsInRole("Admin");
            if (id == null)
            {
                return NotFound();
            }

            ApplicationUser user = await db.Users
                .Include(u => u.Posts)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var friendship = await db.Friends.FirstOrDefaultAsync(f =>
            (f.SenderId == currentUser.Id && f.ReceiverId == user.Id && f.Status == 0) ||
            (f.SenderId == user.Id && f.ReceiverId == currentUser.Id && f.Status == 0));

            ViewBag.IsCurrentUserPending = friendship != null ? 1 : 0;

            var followingFriendship = await db.Friends.FirstOrDefaultAsync(f =>
            (f.SenderId == currentUser.Id && f.ReceiverId == user.Id && f.Status == 1) ||
            (f.SenderId == user.Id && f.ReceiverId == currentUser.Id && f.Status == 1));

            ViewBag.IsCurrentUserFollowing = followingFriendship != null;

            return View(user);
        }







        public async Task<ActionResult> EditProfile(string id)
        {
            ApplicationUser user = db.Users.Find(id);

            //user.AllRoles = GetAllRoles();
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(string id, ApplicationUser newData)
        {
            ApplicationUser user = db.Users.Find(id);
            if (ModelState.IsValid)
            {
                user.UserName = newData.UserName;
                user.IsProfilePublic = newData.IsProfilePublic;
                user.Email = newData.Email;
                user.FirstName = newData.FirstName;
                user.LastName = newData.LastName;
                user.PhoneNumber = newData.PhoneNumber;
                TempData["message"] = "Userul a fost Editat cu succes";
                TempData["messageType"] = "alert-success";
                db.SaveChanges();
            }
            
            return RedirectToAction("Index");
        }

        [Route("EditUser")]
        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = db.Users.Find(id);


            return View(user);
        }
 
        [HttpPost]
        
        public async Task<IActionResult> Edit(string id, ApplicationUser newData)
        {
            ApplicationUser user = db.Users.Find(id);
            if (ModelState.IsValid)
            {
                user.UserName = newData.UserName;
                user.IsProfilePublic = newData.IsProfilePublic;
                user.Email = newData.Email;
                user.FirstName = newData.FirstName;
                user.LastName = newData.LastName;
                user.PhoneNumber = newData.PhoneNumber;
            }

            return RedirectToAction("Index");
        }
        
        [HttpPost]
        public IActionResult Delete(string id)
        {
            var user = db.Users
                         .Include("Posts")
                         .Include("Comments")
                         .Include("ApplicationUserGroups")
                         .Where(u => u.Id == id)
                         .First();


            if (user.Comments.Count > 0)
            {
                foreach (var comment in user.Comments)
                {
                    db.Comments.Remove(comment);
                }
            }




            if (user.Posts.Count > 0)
            {
                foreach (var article in user.Posts)
                {
                    db.Posts.Remove(article);
                }
            }

            if (user.ApplicationUserGroups.Count > 0)
            {
                foreach (var userGroup in user.ApplicationUserGroups)
                {
                    db.ApplicationUserGroups.Remove(userGroup);
                }
            }


            db.ApplicationUsers.Remove(user);
            db.SaveChanges();

            return RedirectToAction("Index");
        }



        [NonAction]
        public IEnumerable<SelectListItem> GetAllRoles()
        {
            var selectList = new List<SelectListItem>();

            var roles = from role in db.Roles
                        select role;

            foreach (var role in roles)
            {
                selectList.Add(new SelectListItem
                {
                    Value = role.Id.ToString(),
                    Text = role.Name.ToString()
                });
            }
            return selectList;
        }
        // sistem de adaugat persoane ca si prieteni

        [HttpPost]
        public async Task<IActionResult> AddFriend(string userId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            if (userId != null && userId != currentUser.Id)
            {
                var existingFriendship = await db.Friends
                    .FirstOrDefaultAsync(f =>
                        (f.SenderId == currentUser.Id && f.ReceiverId == userId) ||
                        (f.SenderId == userId && f.ReceiverId == currentUser.Id));

                if (existingFriendship == null)
                {
                    var newFriendship = new Friend
                    {
                        SenderId = currentUser.Id,
                        ReceiverId = userId,
                        Status = 0 
                    };
                    var sender = await _userManager.FindByIdAsync(userId);

                    var senderUsername = await _userManager.GetUserNameAsync(sender);

                    newFriendship.SenderUsername = senderUsername;

                    db.Friends.Add(newFriendship);
                    await db.SaveChangesAsync();
                }
            }

            return RedirectToAction("Show", new { id = userId });
        }


        public IActionResult Notifications()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var friendRequests = db.Friends
                .Where(f => f.ReceiverId == currentUserId && f.Status == 0)
                .ToList();

            ViewBag.FriendRequests = friendRequests;
            Console.WriteLine("Notificari!");

            return View();
        }





        [HttpPost]
        public async Task<IActionResult> AcceptFriendRequest(string friendRequestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            var friendRequest = await db.Friends
                .FirstOrDefaultAsync(f =>
                    f.ReceiverId == currentUser.Id && f.Status == 0 && f.SenderId == friendRequestId);

            if (friendRequest != null)
            {
                friendRequest.Status = 1; 
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> RejectFriendRequest(string friendRequestId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var friendRequestToDelete = await db.Friends
                    .FirstOrDefaultAsync(f =>
                        f.SenderId == friendRequestId && f.ReceiverId == currentUser.Id && f.Status == 0);

            if (friendRequestToDelete != null)
            {
                db.Friends.Remove(friendRequestToDelete);
                await db.SaveChangesAsync();
            }
            return RedirectToAction("Notifications");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            var currentUser = await _userManager.GetUserAsync(User);

            // pentru a o șterge prietenie
            var friendship = await db.Friends.FirstOrDefaultAsync(f =>
                (f.SenderId == currentUser.Id && f.ReceiverId == friendId && f.Status == 1) ||
                (f.SenderId == friendId && f.ReceiverId == currentUser.Id && f.Status == 1));

            if (friendship != null)
            {
                db.Friends.Remove(friendship);
                await db.SaveChangesAsync();
            }

            return RedirectToAction("Show", new { id = friendId });
        }



    }
}