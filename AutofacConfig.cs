using Autofac;
using Autofac.Extras.DynamicProxy;
using Autofac.Integration.WebApi;
using SSPC.Meeting.AOP.AopInterceptor;
using SSPC.Meeting.AutofacManager;
using SSPC.Meeting.Core.Cache;
using SSPC.Meeting.Core.Data;
using SSPC.Meeting.Data.Data;
using SSPC.Meeting.OAuth.Implementations;
using SSPC.Meeting.OAuth.Interfaces;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using WebGrease;

namespace SSPC.Meeting.WebApi.App_Start
{
    /// <summary>
    /// 
    /// </summary>
    public static class AutofacConfig
    {
        /// <summary>
        /// IOC 
        /// </summary>
        /// <param name="config"></param>
        public static void AutofacIoc(HttpConfiguration config)
        {
            var builder = new ContainerBuilder();
            #region AOP

            builder.Register(c => new RepositoryInterceptor()).InstancePerLifetimeScope();
            builder.Register(c => new ServiceInterceptor()).InstancePerLifetimeScope();
            #endregion

            #region Repository

            string connectStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            builder.Register<string>(c => connectStr);
            builder.Register<IDbContext>(c => new FacDbContext(connectStr)).InstancePerLifetimeScope();
            //builder.RegisterGeneric(typeof(EfRepository)).As(typeof(IEfRepository)).InstancePerLifetimeScope().EnableInterfaceInterceptors().InterceptedBy(typeof(RepositoryInterceptor));
            builder.RegisterType<EfRepository>().As<IEfRepository>().InstancePerLifetimeScope().EnableInterfaceInterceptors().InterceptedBy(typeof(RepositoryInterceptor));
            #endregion

            #region OAuth2.0
            builder.RegisterType<AuthRepository>().As<IAuthRepository>().InstancePerLifetimeScope().EnableInterfaceInterceptors().InterceptedBy(typeof(RepositoryInterceptor));
            builder.RegisterType<IdentityUserStore>().As<IIdentityUserStore>().InstancePerLifetimeScope().EnableInterfaceInterceptors().InterceptedBy(typeof(RepositoryInterceptor));
            #endregion

            #region ApiController
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            #endregion

            #region Services
            var services = Assembly.Load("SSPC.Meeting.Services");
            builder.RegisterAssemblyTypes(services)
                .Where(s => s.Name.EndsWith("Service"))
                .AsImplementedInterfaces().EnableInterfaceInterceptors().InterceptedBy(typeof(ServiceInterceptor)).InstancePerLifetimeScope();
            #endregion

            #region Cache
            builder.RegisterType<MemoryCacheManager>().As<Core.Cache.ICacheManager>().SingleInstance();
            #endregion

            var container = builder.Build();
            if (ContainerManager.Container == null)
            {
                ContainerManager.Container = container;
            }
            config.DependencyResolver = new AutofacWebApiDependencyResolver(ContainerManager.Container);
        }
    }
}