using LogicMonitor.DataSDK.Api;
using LogicMonitor.DataSDK.Model;
using LogicMonitor.DataSDK;
using System.Diagnostics;
using NLog.Layouts;
namespace NLog.Targets.LogicMonitor
{

    [Target("LogicMonitor")]
    public class LogicMonitorTarget : TargetWithContext
    {
        public Layout Interval { get; set; }
        public Layout Batch { get; set; }
        public Layout Company { get; set; }
        public Layout LMAccessKey { get; set; }
        public Layout LMAccessID { get; set; }
        public Layout ResourceName { get; set; }

        private Configuration _config;
        private ApiClient _apiClient;
        private Logs _logs;
        private Resource _resource;
        private int _interval { get; set; }
        private bool _batch { get; set; }

        public LogicMonitorTarget() : base()
        {
            _interval = 10;
            _batch = true;
        }

        public LogicMonitorTarget(Configuration configuration, Resource resource = null, int interval = 10, bool batch = true) : base()
        {
            _config = configuration ??= new Configuration();
            _interval = interval;
            _batch = batch;
            _resource = resource;
        }
        protected override void InitializeTarget()
        {
            base.InitializeTarget();
            string company = Company.ToString();
            string lmAccessID = LMAccessID.ToString();
            string lmAccessKey = LMAccessKey.ToString();
            _batch = Convert.ToBoolean(Batch.ToString());
            _interval = Convert.ToInt32(Interval.ToString());
            _config = new Configuration(company: company, accessID: lmAccessID, accessKey: lmAccessKey);
            _resource = new Resource();
            if (ResourceName != null)
            {
                _resource.Name = ResourceName.ToString();
                _resource.Ids = new Dictionary<string, string>();
                _resource.Ids.Add("system.hostname", ResourceName.ToString());
            }
            _apiClient = new ApiClient(configuration: _config ??= new Configuration());

            _logs = new Logs(batch: _batch, interval: _interval, apiClient: _apiClient);

        }

        protected override void CloseTarget()
        {
            base.CloseTarget();
        }

        protected override void Write(LogEventInfo logEvent)
        {
            try
            {
                Dictionary<string, string> metaData = new Dictionary<string, string>();
                string formattedMessage = logEvent.FormattedMessage;
                if (Activity.Current != null)
                    metaData = LogEventMetaData(Activity.Current);

                var currentLogLevel = logEvent.Level.ToString();
                metaData.Add(Constants.LogLevel, currentLogLevel);

                DateTimeOffset milliseconds = logEvent.TimeStamp;
                long tst = milliseconds.ToUnixTimeMilliseconds();
                _logs.SendLogs(message: formattedMessage, resource: _resource, metadata: metaData, timestamp: tst);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
        }

        private Dictionary<string, string> LogEventMetaData(Activity activity)
        {

            var traceID = activity.TraceId.ToString();
            var spanId = activity.SpanId.ToString();
            var operationName = activity.OperationName.ToString();

            Dictionary<string, string> keyValues = new Dictionary<string, string>();
            keyValues.Add(Constants.TracesKey.TraceId, traceID);
            keyValues.Add(Constants.TracesKey.SpanId, spanId);
            keyValues.Add(Constants.TracesKey.OperationName, operationName);
            return keyValues;
        }

    }
}

