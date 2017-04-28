using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using SmartGallery.Data.Repositories;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(SmartGallery.Web.App_Start.NinjectWebCommon), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethodAttribute(typeof(SmartGallery.Web.App_Start.NinjectWebCommon), "Stop")]

namespace SmartGallery.Web.App_Start
{
    using System;
    using System.Web;

    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    using Ninject;
    using Ninject.Web.Common;

    public static class NinjectWebCommon 
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() 
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }
        
        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }
        
        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                kernel.Bind<CommentRepository>().ToMethod(ctx =>
                {
                    return new CommentRepository(
                        CloudConfigurationManager.GetSetting("documentdb:host"),
                        CloudConfigurationManager.GetSetting("documentdb:key"),
                        CloudConfigurationManager.GetSetting("documentdb:dbname"),
                        CloudConfigurationManager.GetSetting("documentdb:commentcollection")
                        );
                }).InRequestScope();

                kernel.Bind<CloudStorageAccount>().ToMethod(ctx =>
                {
                    return CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("storage:connectionstring"));
                }).InSingletonScope();
                
                RegisterServices(kernel);
                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
        }        
    }
}
