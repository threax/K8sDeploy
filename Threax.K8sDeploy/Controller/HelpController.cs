﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Threax.K8sDeploy.Controller
{
    class HelpController : IController
    {
        private ILogger logger;

        public HelpController(ILogger<HelpController> logger)
        {
            this.logger = logger;
        }

        public async Task Run()
        {
            logger.LogInformation("Help");
        }
    }
}
