using System;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;

namespace MyWindowsService
{
    public partial class MyService : ServiceBase
    {
        public MyService()
        {
            InitializeComponent();
        }


        protected override void OnStart(string[] args)
        {
            //sign _sign = new sign();
            WebservicePost _webservice = new WebservicePost();
            _webservice.init();
        }

        protected override void OnStop()
        {

        }
    }
}
