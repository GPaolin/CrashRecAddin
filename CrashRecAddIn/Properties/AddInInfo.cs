using Mono.Addins;

// Declares that this assembly is an add-in
[assembly: Addin("CrashRecAddIn", "1.0.1")]

// Declares that this add-in depends on the scada v1.0 add-in root
[assembly: AddinDependency("::scada", "1.0")]

[assembly: AddinName("CrashRecAddIn")]
[assembly: AddinDescription("Crash Recorder Trigger Manager")]