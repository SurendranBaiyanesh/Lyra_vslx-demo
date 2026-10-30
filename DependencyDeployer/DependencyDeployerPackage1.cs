namespace DependencyDeployer
{
    using System;

    internal sealed partial class PackageGuids
    {
        public const string guidDependencyDeployerPackageString = "a3f5b7d2-1c4e-4f8a-9b6d-0e2c8f1a7b35";
        public static Guid guidDependencyDeployerPackage = new Guid(guidDependencyDeployerPackageString);

        public const string guidDependencyDeployerCmdSetString = "d81e4c6a-5b2f-4a9e-8c3d-7f0b1e6a2c94";
        public static Guid guidDependencyDeployerCmdSet = new Guid(guidDependencyDeployerCmdSetString);
    }

    internal sealed partial class PackageIds
    {
        public const int DeployCommand = 0x0100;
    }
}
