# Threax.K8sDeploy
Deploy apps to k8s in a standardized way.

## Adding Configuration to App
You can add the configuration objects to your app's config by doing the following. You do not have to do this, but it will provide k8s deploy intellisense when editing your app config files if you install it.

Add the `Threax.DeployConfig` library to your project. You can do it like the following, but make sure to get the latest version.
```
<PackageReference Include="Threax.DeployConfig" Version="1.0.0" GeneratePathProperty="true" />
```
`GeneratePathProperty` will let us copy the documentation files.

Next add steps to copy the documentation files to the output directory so the schema binder works:
```
<Target Name="CopyDocs" AfterTargets="Build">
    <Copy SourceFiles="$(PkgThreax_DeployConfig)\lib\netstandard2.0\Threax.DeployConfig.xml" DestinationFolder="$(OutDir)" />
</Target>
```
This will copy the documentation files to the output directory on build. This will make the schema binder work correctly. If you already have this section in your csproj you can add just the copy element to it.

Add the following to the constructor in Startup.cs.
```
Configuration.Define("Deploy", typeof(Threax.DeployConfig.DeploymentConfig));
```

Finally run your app in `Update Config Schema` mode to update your config schema.
