// See https://aka.ms/new-console-template for more information

using ControlBee;
using log4net.Config;

XmlConfigurator.Configure(new FileInfo("log4net.config"));

Console.WriteLine("Hello, World!");

var obj = new Class1();
Console.WriteLine(obj.Foo());
