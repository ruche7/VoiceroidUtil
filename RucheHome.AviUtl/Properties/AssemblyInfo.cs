using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Markup;

// アセンブリに関する一般情報は以下の属性セットをとおして制御されます。
// アセンブリに関連付けられている情報を変更するには、
// これらの属性値を変更してください。
[assembly: AssemblyTitle("RucheHome.AviUtl")]
[assembly: AssemblyDescription("The library for AviUtl.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("ruche-home")]
[assembly: AssemblyProduct("RucheHome.AviUtl")]
[assembly: AssemblyCopyright("Copyright (C) 2016 ruche.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// ComVisible を false に設定すると、その型はこのアセンブリ内で COM コンポーネントから 
// 参照不可能になります。COM からこのアセンブリ内の型にアクセスする場合は、
// その型の ComVisible 属性を true に設定してください。
[assembly: ComVisible(false)]

// このプロジェクトが COM に公開される場合、次の GUID が typelib の ID になります
[assembly: Guid("ef44d39c-4106-4e7b-ae0d-9a3b41de6eac")]

// アセンブリのバージョン情報は次の 4 つの値で構成されています:
//
//      メジャー バージョン
//      マイナー バージョン
//      ビルド番号
//      Revision
//
// すべての値を指定するか、下のように '*' を使ってビルドおよびリビジョン番号を 
// 既定値にすることができます:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.8.0.*")]

// XML名前空間
[assembly: XmlnsDefinition(
    @"http://schemas.ruche-home.net/xaml/aviutl",
    @"RucheHome.AviUtl")]
[assembly: XmlnsDefinition(
    @"http://schemas.ruche-home.net/xaml/aviutl",
    @"RucheHome.AviUtl.ExEdit")]
