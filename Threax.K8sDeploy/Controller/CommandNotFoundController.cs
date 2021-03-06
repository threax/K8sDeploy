﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Threax.K8sDeploy.Controller
{
    class CommandNotFoundController : IController
    {
        private ILogger logger;

        public CommandNotFoundController(ILogger<CommandNotFoundController> logger)
        {
            this.logger = logger;
        }

        public async Task Run()
        {
            logger.LogInformation("Command not found.");
        }
    }
}
