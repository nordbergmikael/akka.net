﻿using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.Remote.Routing;
using Akka.Routing;
using Akka.Util.Internal;

namespace Akka.Remote
{
    /// <summary>
    /// INTERNAL API
    /// 
    /// Used for deployment of actors on remote systems
    /// </summary>
    internal class RemoteDeployer : Deployer
    {
        public RemoteDeployer(Settings settings) : base(settings)
        {
        }

        public override Deploy ParseConfig(string key, Config config)
        {
            var deploy = base.ParseConfig(key, config);
            if (deploy == null) return null;

            var remote = deploy.Config.GetString("remote");

            ActorPath actorPath;
            if(ActorPath.TryParse(remote, out actorPath))
            {
                var address = actorPath.Address;
                //can have remotely deployed routers that remotely deploy routees
                return CheckRemoteRouterConfig(deploy.Copy(scope: new RemoteScope(address)));
            }
            
            if (!string.IsNullOrWhiteSpace(remote))
                throw new ConfigurationException(string.Format("unparseable remote node name [{0}]", remote));

            return CheckRemoteRouterConfig(deploy);
        }

        /// <summary>
        /// Used to determine if a given <see cref="deploy"/> is an instance of <see cref="RemoteRouterConfig"/>.
        /// </summary>
        private static Deploy CheckRemoteRouterConfig(Deploy deploy)
        {
            var nodes = deploy.Config.GetStringList("target.nodes").Select(Address.Parse).ToList();
            if (nodes.Any() && deploy.RouterConfig != RouterConfig.NoRouter)
            {
                if (deploy.RouterConfig is Pool)
                    return
                        deploy.Copy().WithRouterConfig(new RemoteRouterConfig(deploy.RouterConfig.AsInstanceOf<Pool>(), nodes));
                return deploy.Copy(scope: Deploy.NoScopeGiven);
            }
            else
            {
                //TODO: return deploy;
                return deploy.Copy(scope: Deploy.NoScopeGiven);
            }
        }
    }
}
