using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Qml.Net;
using Qml.Net.Runtimes;

namespace Features
{
    class Program
    {
        static async Task Main(string[] args)
        {
            RuntimeManager.DiscoverOrDownloadSuitableQtRuntime();
            
            QQuickStyle.SetStyle("Material");

            var (mainQml, items) = await QmlResourceManager.BuildResources("Main.qml", args);

            using (var application = new QGuiApplication(args))
            {
                using (items)
                {
                    using var qmlEngine = new QQmlApplicationEngine();
                    Qml.Net.Qml.RegisterType<SignalsModel>("Features");
                    Qml.Net.Qml.RegisterType<NotifySignalsModel>("Features");
                    Qml.Net.Qml.RegisterType<AsyncAwaitModel>("Features");
                    Qml.Net.Qml.RegisterType<NetObjectsModel>("Features");
                    Qml.Net.Qml.RegisterType<DynamicModel>("Features");
                    Qml.Net.Qml.RegisterType<CalculatorModel>("Features");
                    Qml.Net.Qml.RegisterType<CollectionsModel>("Features");

                    qmlEngine.Load(mainQml);

                    application.Exec();
                }
            }

            new Application().Shutdown();
        }
    }
}
