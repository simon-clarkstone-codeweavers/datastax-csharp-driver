﻿using System;
using System.Diagnostics;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Dse.Test.Integration.TestBase
{
    public class RemoteCcmProcessExecuter : CcmProcessExecuter
    {
        private readonly string _user;
        private readonly string _ip;
        private readonly int _port;
        private readonly string _password;
        private SshClient _sshClient;

        public RemoteCcmProcessExecuter(string ip, string user, string password, int port = 22)
        {
            _user = user;
            _ip = ip;
            _password = password;
            _port = port;
        }


        public override ProcessOutput ExecuteCcm(string args, int timeout = 90000, bool throwOnProcessError = true)
        {
            var executable = GetExecutable(ref args);
            Trace.TraceInformation(executable + " " + args);

            var output = new ProcessOutput();
            if (_sshClient == null)
            {
                Trace.TraceInformation("Connecting ssh client...");
                var kauth = new KeyboardInteractiveAuthenticationMethod(_user);
                var pauth = new PasswordAuthenticationMethod(_user, _password);

                var connectionInfo = new ConnectionInfo(_ip, _port, _user, kauth, pauth);

                kauth.AuthenticationPrompt += delegate(object sender, AuthenticationPromptEventArgs e)
                {
                    foreach (var prompt in e.Prompts)
                    {
                        if (prompt.Request.ToLowerInvariant().StartsWith("password"))
                        {
                            prompt.Response = _password;
                        }
                    }
                };
                _sshClient = new SshClient(connectionInfo);
            }
            if (!_sshClient.IsConnected)
                _sshClient.Connect();

            var result = _sshClient.RunCommand(string.Format(@"{0} {1}", executable, args));
            output.ExitCode = result.ExitStatus;
            output.OutputText.Append(result.Result);

            if (throwOnProcessError)
            {
                ValidateOutput(output);
            }
            return output;
        }

        protected override string GetExecutable(ref string args)
        {
            return LocalCcmProcessExecuter.CcmCommandPath;
        }

        ~RemoteCcmProcessExecuter()
        {
            if (_sshClient != null && _sshClient.IsConnected) 
                _sshClient.Disconnect();
        }
    }
}
