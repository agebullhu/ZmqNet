using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Agebull.Common;
using Agebull.Common.Configuration;
using Agebull.Common.Logging;
using Agebull.ZeroNet.ZeroApi;
using Gboxt.Common.DataModel;
using Microsoft.Extensions.Configuration;
using ZeroMQ;
using ZeroMQ.lib;

namespace Agebull.ZeroNet.Core
{
    /// <summary>
    ///     站点应用
    /// </summary>
    public partial class ZeroApplication
    {
        #region Console

        /// <summary>
        ///     命令行方式管理
        /// </summary>
        public static void CommandConsole()
        {
            while (true)
            {
                var cmd = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(cmd))
                    continue;
                switch (cmd.Trim().ToLower())
                {
                    case "quit":
                    case "exit":
                        Shutdown();
                        return;
                    case "start":
                        Start();
                        continue;
                }

                var words = cmd.Split(' ', '\t', '\r', '\n');
                if (words.Length == 0)
                {
                    ZeroTrace.WriteLine("请输入正确命令");
                    continue;
                }

                var result = SystemManager.CallCommand(words);
                if (result.InteractiveSuccess)
                    ZeroTrace.WriteInfo("Console", result.TryGetValue(ZeroFrameType.TextValue, out var value)
                        ? value
                        : result.State.Text());
                else
                    ZeroTrace.WriteError("Console", result.TryGetValue(ZeroFrameType.TextValue, out var value)
                        ? value
                        : result.State.Text());
            }
        }

        #endregion


        #region State

        /// <summary>
        ///     ZeroCenter是否正在运行
        /// </summary>
        public static bool ZerCenterIsRun => ZerCenterStatus == ZeroCenterState.Run;

        /// <summary>
        ///     服务器状态
        /// </summary>
        public static ZeroCenterState ZerCenterStatus { get; internal set; }

        /// <summary>
        ///     运行状态（本地与服务器均正常）
        /// </summary>
        public static bool CanDo =>
            (ApplicationState == StationState.BeginRun || ApplicationState == StationState.Run) &&
            ZerCenterStatus == ZeroCenterState.Run;

        /// <summary>
        ///     运行状态（本地未关闭）
        /// </summary>
        public static bool IsAlive => ApplicationState < StationState.Destroy;

        /// <summary>
        ///     已关闭
        /// </summary>
        public static bool IsDisposed => ApplicationState == StationState.Disposed;

        /// <summary>
        ///     已关闭
        /// </summary>
        public static bool IsClosed => ApplicationState >= StationState.Closed;

        /// <summary>
        ///     是否正在运行
        /// </summary>
        public static bool InRun => ApplicationState == StationState.Run;

        /// <summary>
        ///     运行状态
        /// </summary>
        internal static int _appState;

        /// <summary>
        ///     状态
        /// </summary>
        public static int ApplicationState
        {
            get => _appState;
            internal set => Interlocked.Exchange(ref _appState, value);
        }

        #endregion


        #region Flow

        #region Option

        /// <summary>
        ///     配置校验,作为第一步
        /// </summary>
        public static void CheckOption()
        {
            ZeroTrace.Initialize();
            CheckConfig();
            InitializeDependency();
        }


        /// <summary>
        ///     设置LogRecorder的依赖属性(内部使用)
        /// </summary>
        private static void InitializeDependency()
        {
            ApiContext.MyRealName = Config.RealName;
            ApiContext.MyServiceKey = Config.ServiceKey;
            ApiContext.MyServiceName = Config.ServiceName;
            LogRecorder.GetMachineNameFunc = () => Config.ServiceName;
            LogRecorder.GetUserNameFunc = () => ApiContext.Customer?.Account ?? "Unknow";
            LogRecorder.GetRequestIdFunc = () => ApiContext.RequestContext?.RequestId ?? Guid.NewGuid().ToString();

            AddInImporter.Importe();
            AddInImporter.Instance.Initialize();
            LogRecorder.Initialize();
        }

        private static string GetHostIps()
        {
            var ips = new StringBuilder();
            var first = true;
            foreach (var address in Dns.GetHostAddresses(Config.ServiceName))
            {
                if (address.IsIPv4MappedToIPv6 || address.IsIPv6LinkLocal || address.IsIPv6Multicast ||
                    address.IsIPv6SiteLocal || address.IsIPv6Teredo)
                    continue;
                var ip = address.ToString();
                if (ip == "127.0.0.1" || ip == "127.0.1.1" || ip == "::1" || ip == "-1")
                    continue;
                if (first)
                    first = false;
                else
                    ips.Append(" , ");
                ips.Append(ip);
            }

            return ips.ToString();
        }

        /// <summary>
        ///     发现
        /// </summary>
        public static void Discove(Assembly assembly = null)
        {
            var discover = new ZeroDiscover { Assembly = assembly ?? Assembly.GetCallingAssembly() };
            ZeroTrace.WriteInfo("Discove", discover.Assembly.FullName);
            discover.FindApies();
            discover.FindZeroObjects();
        }

        #endregion

        #region Initialize

        /// <summary>
        ///     初始化
        /// </summary>
        public static void Initialize()
        {
            RegistZeroObject(ZeroConnectionPool.CreatePool());
            AddInImporter.Instance.AutoRegist();

            ApplicationState = StationState.Initialized;
            OnZeroInitialize();
        }

        #endregion

        #region Run

        /// <summary>
        ///     启动
        /// </summary>
        private static void Start()
        {
            ZContext.Initialize();

            using (OnceScope.CreateScope(Config))
            {
                ApplicationState = StationState.Start;
                Task.Factory.StartNew(SystemMonitor.Monitor);
                JoinCenter();
            }
            SystemMonitor.WaitMe();
        }

        /// <summary>
        ///     运行
        /// </summary>
        public static void Run()
        {
            Start();
        }

        /// <summary>
        ///     执行并等待
        /// </summary>
        public static void RunAwaite()
        {
            Console.CancelKeyPress += OnCancelKeyPress;
            Console.WriteLine("Application start...");
            Start();
            Task.Factory.StartNew(WaitTask).Wait();
        }

        /// <summary>
        ///     应用程序等待结果的信号量对象
        /// </summary>
        private static readonly SemaphoreSlim WaitToken = new SemaphoreSlim(0, 1);

        /// <summary>
        ///     执行直到连接成功
        /// </summary>
        private static void WaitTask()
        {
            Console.WriteLine("Application started. Press Ctrl+C to shutdown.");
            WaitToken.Wait();
        }

        /// <summary>
        ///     进入系统侦听
        /// </summary>
        internal static bool JoinCenter()
        {
            ZeroTrace.WriteLine($"try connect zero center ({Config.ZeroManageAddress})...");
            if (!SystemManager.PingCenter())
            {
                ApplicationState = StationState.Failed;
                ZerCenterStatus = ZeroCenterState.Failed;
                ZeroTrace.WriteError("JoinCenter", "zero center can`t connection.");
                return false;
            }

            ZerCenterStatus = ZeroCenterState.Run;
            ApplicationState = StationState.BeginRun;
            if (!SystemManager.HeartJoin())
            {
                ApplicationState = StationState.Failed;
                ZerCenterStatus = ZeroCenterState.Failed;
                ZeroTrace.WriteError("JoinCenter", "zero center can`t connection.");
                return false;
            }

            if (!SystemManager.LoadAllConfig())
            {
                ApplicationState = StationState.Failed;
                ZerCenterStatus = ZeroCenterState.Failed;
                return false;
            }

            ZeroTrace.WriteLine("be connected successfully,start local stations.");
            OnZeroStart();
            return true;
        }

        #endregion

        #region Destroy

        /// <summary>
        ///     关闭
        /// </summary>
        public static void Shutdown()
        {
            ZeroTrace.WriteLine("Begin shutdown...");
            switch (ApplicationState)
            {
                case StationState.Destroy:
                    return;
                case StationState.BeginRun:
                case StationState.Run:
                    OnZeroEnd();
                    break;
                case StationState.Failed:
                    SystemManager.HeartLeft();
                    break;
            }
            ApplicationState = StationState.Destroy;
            if (GlobalObjects.Count > 0)
                GlobalSemaphore.Wait();
            OnZeroDestory();
            SystemManager.Destroy();
            LogRecorder.Shutdown();
            SystemMonitor.WaitMe();
            ZContext.Destroy();
            ZeroTrace.WriteLine("Application shutdown ,see you late.");
            ApplicationState = StationState.Disposed;
            WaitToken.Release();
        }

        private static void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Shutdown();
        }

        #endregion

        #endregion
    }
}