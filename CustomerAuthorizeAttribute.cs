using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Security.Principal;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Properties;
using SSPC.Meeting.AutofacManager;
using SSPC.Meeting.Core.Cache;
using System.Web.Http.WebHost;
using Microsoft.Owin.Security.Cookies;
using SSPC.Meeting.Core.Data;
using SSPC.Meeting.Core.Domain.CommonModels;
using SSPC.Meeting.Core.Domain.Models.DataModels;
using SSPC.Meeting.Core.Domain.Models.IdentityEntities;
using SSPC.Meeting.Core.Domain.ViewModels;

namespace SSPC.Meeting.MeetingApi.App_Start
{
    /// <summary>
    /// 自定义验证
    /// </summary>
    public class CustomerAuthorizeAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="actionContext"></param>
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var cache = ContainerManager.Resolve<ICacheManager>();
            var adName = actionContext?.RequestContext?.Principal?.Identity?.Name;
            if (!string.IsNullOrEmpty(adName))
            {
                var workUser = cache.Get<WorkUser>(adName);
                if (workUser == null)
                {
                    var rep = ContainerManager.Resolve<IEfRepository>();
                    var user = rep.FirstOrDefault<UserModel>(s => s.UserAccount == adName);
                    if (user != null)
                    {
                        var userViewModel = new UserViewModel
                        {
                            UserCode = user.UserCode,
                            CompanyCode = user.CompanyCode,
                            CreateTime = user.CreateTime,
                            CreateUser = user.CreateUser,
                            Id = user.Id,
                            UserAccount = user.UserAccount,
                            UserAddress = user.UserAddress,
                            UserDept = user.UserDept,
                            UserMail = user.UserMail,
                            UserName = user.UserName,
                            UserTel = user.UserTel,
                            CompanyName = rep.FirstOrDefault<CompanyInfo>(s => s.CompanyCode == user.CompanyCode)?.CompanyName,
                            UserCompany = user.UserCompany
                            
                        };
                        var userRole = rep.Where<UserRole>(s => s.UserCode == user.UserCode);
                        var isSuperAdmin = userRole.Any(s => s.RoleId == "EF153464-0340-494F-AB72-A734733CF996") ? 1 : 0;
                        var isAreaAdmin = userRole.Any(s =>
                            s.RoleId == "D07599DC-0D53-4C73-9ADB-33F0D9AE9426" ||
                            s.RoleId == "7DB07F89-4475-4CA1-AB39-A563DE901D4C" ||
                            s.RoleId == "61359D91-52F2-42D7-B2AE-A70ACFD39653")
                            ? 1
                            : 0;
                        workUser = new WorkUser
                        {
                            UserInfo = userViewModel,
                            UserRoles = userRole,
                            IsSuperAdmin = isSuperAdmin,
                            IsAreaAdmin = isAreaAdmin
                        };
                        ContainerManager.Resolve<ICacheManager>().Set(user.UserCode, workUser);

                        base.OnAuthorization(actionContext);
                    }
                    else
                    {
                        actionContext.Response = actionContext.ControllerContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized,"无权限");
                    }

                }
            }
            else
            {
                actionContext.Response = actionContext.ControllerContext.Request.CreateErrorResponse(HttpStatusCode.Unauthorized, "无权限");
            }
            
        }
    }
}