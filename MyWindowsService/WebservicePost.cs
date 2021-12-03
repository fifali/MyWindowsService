using System;
using System.ServiceProcess;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using System.IO;
using Newtonsoft.Json.Linq;
using swiftpass.utils;
using System.Configuration;
using System.Web.Services.Description;
using System.CodeDom;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Drawing;
using System.Security.Policy;
//using System.CodeDom;
//using Microsoft.CSharp;
//using System.CodeDom.Compiler;

namespace MyWindowsService
{
    public class WebservicePost
    {
        static HttpListener httpobj;
        static Gy.HelloSAM tt = new Gy.HelloSAM();
        static Qz.Dcipservice cc = new Qz.Dcipservice();
        public void init()
        {
            //提供一个简单的、可通过编程方式控制的 HTTP 协议侦听器。此类不能被继承。
            httpobj = new HttpListener();
            //定义url及端口号，通常设置为配置文件
            //httpobj.Prefixes.Add("http://192.168.41.75:8082/PostTest/");
            httpobj.Prefixes.Add(ConfigurationManager.AppSettings["url"]);
            //启动监听器
            httpobj.Start();
            //异步监听客户端请求，当客户端的网络请求到来时会自动执行Result委托
            //该委托没有返回值，有一个IAsyncResult接口的参数，可通过该参数获取context对象
            httpobj.BeginGetContext(Result, null);
            //Console.WriteLine($"服务端初始化完毕，正在等待客户端请求,时间：{DateTime.Now.ToString()}\r\n");
            //Console.ReadKey();
        }

        #region 实例化WebServices
        /// <summary>
        /// 实例化WebServices
        /// </summary>
        /// <param name="url">WebServices地址</param>
        /// <param name="methodname">调用的方法</param>
        /// <param name="args">把webservices里需要的参数按顺序放到这个object[]里</param>
        public static object InvokeWebService(string url, string methodname, object[] args)
        {
            //这里的namespace是需引用的webservices的命名空间，我没有改过，也可以使用。也可以加一个参数从外面传进来。
            string @namespace = "client";

            try
            {
                //获取WSDL
                WebClient wc = new WebClient();
                Stream stream = wc.OpenRead(url + "?WSDL");
                ServiceDescription sd = ServiceDescription.Read(stream);
                string classname = sd.Services[0].Name;
                ServiceDescriptionImporter sdi = new ServiceDescriptionImporter();
                sdi.AddServiceDescription(sd, "", "");
                CodeNamespace cn = new CodeNamespace(@namespace);

                //生成客户端代理类代码
                CodeCompileUnit ccu = new CodeCompileUnit();
                ccu.Namespaces.Add(cn);
                sdi.Import(cn, ccu);
                CSharpCodeProvider csc = new CSharpCodeProvider();
                //ICodeCompiler icc = csc.CreateCompiler();

                //设定编译参数
                CompilerParameters cplist = new CompilerParameters();
                cplist.GenerateExecutable = false;//动态编译后的程序集不生成可执行文件
                cplist.GenerateInMemory = true;//动态编译后的程序集只存在于内存中，不在硬盘的文件上
                cplist.ReferencedAssemblies.Add("System.dll");
                cplist.ReferencedAssemblies.Add("System.XML.dll");
                cplist.ReferencedAssemblies.Add("System.Web.Services.dll");
                cplist.ReferencedAssemblies.Add("System.Data.dll");

                //编译代理类
                CompilerResults cr = csc.CompileAssemblyFromDom(cplist, ccu);
                if (true == cr.Errors.HasErrors)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    foreach (System.CodeDom.Compiler.CompilerError ce in cr.Errors)
                    {
                        sb.Append(ce.ToString());
                        sb.Append(System.Environment.NewLine);
                    }

                    throw new Exception(sb.ToString());
                }

                //生成代理实例，并调用方法
                System.Reflection.Assembly assembly = cr.CompiledAssembly;
                Type t = assembly.GetType(@namespace + "." + classname, true, true);
                object obj = Activator.CreateInstance(t);
                System.Reflection.MethodInfo mi = t.GetMethod(methodname);

                //注：method.Invoke(o, null)返回的是一个Object,如果你服务端返回的是DataSet,这里也是用(DataSet)method.Invoke(o, null)转一下就行了,method.Invoke(0,null)这里的null可以传调用方法需要的参数,string[]形式的
                return mi.Invoke(obj, args);
            }
            catch
            {
                return null;
            }
        }
        #endregion

        //#region 动态调用webservice
        //public static void()
        //{
        //    string urls = "http://localhost:3182/Service1.asmx?WSDL";//这个地址可以写在Config文件里面，这里取出来就行了.在原地址后面加上： ?WSDL
        //        string methods = "GetPersons";

        //    String[] item = (String[])Webservice.InvokeWebService(urls, methods, null);

        //    foreach (string str in item)
        //        Console.WriteLine(str);
        //}
        //#endregion

        private static void Result(IAsyncResult ar)
        {
            //当接收到请求后程序流会走到这里

            //继续异步监听
            httpobj.BeginGetContext(Result, null);
            var guid = Guid.NewGuid().ToString();
            //Console.ForegroundColor = ConsoleColor.White;
            //Console.WriteLine($"接到新的请求:{guid},时间：{DateTime.Now.ToString()}");
            //获得context对象
            var context = httpobj.EndGetContext(ar);
            var request = context.Request;
            var response = context.Response;
            ////如果是js的ajax请求，还可以设置跨域的ip地址与参数
            //context.Response.AppendHeader("Access-Control-Allow-Origin", "*");//后台跨域请求，通常设置为配置文件
            //context.Response.AppendHeader("Access-Control-Allow-Headers", "ID,PW");//后台跨域参数设置，通常设置为配置文件
            //context.Response.AppendHeader("Access-Control-Allow-Method", "post");//后台跨域请求设置，通常设置为配置文件
            context.Response.ContentType = "text/plain;charset=UTF-8";//告诉客户端返回的ContentType类型为纯文本格式，编码为UTF-8
            context.Response.AddHeader("Content-type", "text/plain");//添加响应头信息
            context.Response.ContentEncoding = Encoding.UTF8;
            string returnObj = null;//定义返回客户端的信息
            if (request.HttpMethod == "POST" && request.InputStream != null)
            {
                //处理客户端发送的请求并返回处理信息
                returnObj = HandleRequest(request, response);
            }
            else
            {
                returnObj = $"不是post请求或者传过来的数据为空";
            }
            var returnByteArr = Encoding.UTF8.GetBytes(returnObj);//设置客户端返回信息的编码
            try
            {
                using (var stream = response.OutputStream)
                {
                    //把处理信息返回到客户端
                    stream.Write(returnByteArr, 0, returnByteArr.Length);
                }
            }
            catch (Exception ex)
            {
                //Console.ForegroundColor = ConsoleColor.Red;
                //Console.WriteLine($"网络蹦了：{ex.ToString()}");
            }
            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine($"请求处理完成：{guid},时间：{ DateTime.Now.ToString()}\r\n");
        }
   
        private static string HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            string _resault = null;
            string _data = null;
            string data = null;
            string _type = null;


            string _rettext = null;
            try
            {
                var byteList = new List<byte>();
                var byteArr = new byte[2048];
                int readLen = 0;
                int len = 0;
                //接收客户端传过来的数据并转成字符串类型
                do
                {
                    readLen = request.InputStream.Read(byteArr, 0, byteArr.Length);
                    len += readLen;
                    byteList.AddRange(byteArr);
                } while (readLen != 0);
                data = Encoding.UTF8.GetString(byteList.ToArray(), 0, len);

                //获取得到数据data可以进行其他操作
                var jObject = JObject.Parse(data);
                if (jObject.Property("PostType") == null)
                {
                    #region 找不到PostType标签返回
                    _rettext = "找不到PostType标签";
                    response.StatusDescription = "200";
                    response.StatusCode = 200;
                    return _rettext;
                    #endregion
                }
                if (jObject.Property("PostText") == null)
                {
                    #region 找不到PostText标签返回
                    _rettext = "找不到PostText标签";
                    response.StatusDescription = "200";
                    response.StatusCode = 200;
                    return _rettext;
                    #endregion
                }
                _type = jObject["PostType"].ToString();
                _data = jObject["PostText"].ToString();
                if (_type == "1")//杭州市医保进销存
                {
                    _resault = tt.sayHi(_data);
                }
                else if (_type == "2")//衢州医保
                {
                    string _ydbm = null;
                    string _method = null;
                    string _outparam = "";
                    _method = jObject["Method"].ToString();

                    var jObject2 = JObject.Parse(_data);

                    if (jObject2.Property("ydbm") == null)
                    {
                        #region 找不到PostType标签返回
                        _rettext = "找不到ydbm标签";
                        response.StatusDescription = "200";
                        response.StatusCode = 200;
                        return _rettext;
                        #endregion
                    }
                    _ydbm = jObject2["ydbm"].ToString();
                    _resault = cc.userTransApplication(_method, 0, true, _data, _outparam, _ydbm, "");
                }
                else if (_type == "3")//根据URL地址生成图片BASE64数据
                {
                    string _url = "";
                    _url = _data;

                    if (_url == null)
                    {
                        #region 找不到图片路径返回
                        _rettext = "找不到图片路径";
                        response.StatusDescription = "200";
                        response.StatusCode = 200;
                        return _rettext;
                        #endregion
                    }
                    HttpWebRequest request1 = (HttpWebRequest)WebRequest.Create(_url);
                    WebResponse response1 = request1.GetResponse();//获得响应
                    using (Image image = Image.FromStream(response1.GetResponseStream()))
                    {
                        using (MemoryStream m = new MemoryStream())
                        {
                            image.Save(m, image.RawFormat);
                            byte[] imageBytes = m.ToArray();

                            // Convert byte[] to Base64 String
                            string base64String = Convert.ToBase64String(imageBytes);
                            _resault = base64String;
                            _resault = _resault.Substring(_resault.IndexOf(',') + 1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.StatusDescription = "200";
                response.StatusCode = 200;
                return "在接收数据时发生错误:" + ex.Message.ToString();//把服务端错误信息直接返回可能会导致信息不安全，此处仅供参考
            }
            response.StatusDescription = "200";//获取或设置返回给客户端的 HTTP 状态代码的文本说明。
            response.StatusCode = 200;// 获取或设置返回给客户端的 HTTP 状态代码。
                                      //Console.ForegroundColor = ConsoleColor.Green;
                                      //Console.WriteLine($"接收数据完成:{data.Trim()},时间：{DateTime.Now.ToString()}");
            return _resault;
        }
    }
}
