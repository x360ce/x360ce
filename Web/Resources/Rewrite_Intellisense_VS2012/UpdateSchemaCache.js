//
// Helper script to update the Visual Studio XML Cache with rewrite schema
//
 
/*
 * This tool is provided by Ruslan Yakushev (http://ruslany.net) under the Microsoft Public License
 * (http://www.microsoft.com/opensource/licenses.mspx).
 * Updated for Visual Studio 2012 by Michal A. Valasek (http://www.rider.cz, http://www.aspnet.cz/)
 *
 * This license governs use of the accompanying software. If you use the software, you accept this license.
 * If you do not accept the license, do not use the software.
 *
 * Definitions
 * The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here
 * as under U.S. copyright law. A "contribution" is the original software, or any additions or changes to the
 *  software. A "contributor" is any person that distributes its contribution under this license.
 *  "Licensed patents" are a contributor's patent claims that read directly on its contribution.
 *  
 *  Grant of Rights
 *  (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations
 *  in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to
 *  reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution
 *  or any derivative works that you create.
 *  (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations
 *  in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free license under its
 *  licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its
 *  contribution in the software or derivative works of the contribution in the software.
 *  
 *  Conditions and Limitations
 *  (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo,
 *  or trademarks.
 *  (B) If you bring a patent claim against any contributor over patents that you claim are infringed by
 *  the software, your patent license from such contributor to the software ends automatically.
 *  (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark,
 *  and attribution notices that are present in the software.
 *  (D) If you distribute any portion of the software in source code form, you may do so only under this
 *  license by including a complete copy of this license with your distribution. If you distribute any
 *  portion of the software in compiled or object code form, you may only do so under a license that
 *  complies with this license.
 *  (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express
 *  warranties, guarantees, or conditions. You may have additional consumer rights under your local laws
 *  which this license cannot change. To the extent permitted under your local laws, the contributors
 *  exclude the implied warranties of merchantability, fitness for a particular purpose and non-infringement.
 */
 
function UpdateSchemaFile(schemaFileName) {
    var rewriteSchemaFile = "rewrite.xsd";
    var dotNetSchemaDir = vs11CommonTools.substring(0, vs11CommonTools.indexOf("Common7\\Tools")) + "Xml\\Schemas\\";
    var dotNetSchemaFile = dotNetSchemaDir + schemaFileName;
 
    var sourceXml = new ActiveXObject("msxml2.DOMDocument.3.0");
    var targetXml = new ActiveXObject("msxml2.DOMDocument.3.0");
 
    if (sourceXml.load(rewriteSchemaFile) == 0) {
        WScript.Echo(" Failed to open file " + rewriteSchemaFile + ".");
        return;
    }
 
    if (targetXml.load(dotNetSchemaFile) == 0) {
        WScript.Echo("Failed to open file " + dotNetSchemaFile + ". Make sure that the script is run in the elevated command prompt.");
        return;
    }
 
    // Check if the <rewrite> element is not defined in the DotNetConfig.xsd
    var rewriteNode = rewriteNode = targetXml.selectSingleNode("//xs:schema/xs:element[@name='system.webServer']/xs:complexType/xs:choice/xs:element[@name='rewrite']");
    if (rewriteNode != null) {
        rewriteNode.parentNode.removeChild(rewriteNode);
    }
 
    rewriteNode = sourceXml.selectSingleNode("//xs:schema/xs:element[@name='rewrite']");
    if (rewriteNode == null) {
        WScript.Echo("The definition for the <rewrite> element is not found in the file " + rewriteSchemaFile + ".");
        return;
    }
 
    var systemWebServerNode = targetXml.selectSingleNode("//xs:schema/xs:element[@name='system.webServer']/xs:complexType/xs:choice");
    if (systemWebServerNode == null) {
        WScript.Echo("The definition for the <system.webServer> element is not found in the file " + dotNetSchemaFile + ".");
        return;
    }
 
    // Add the <rewrite> element definition to the schema
    systemWebServerNode.appendChild(rewriteNode);
 
    // Make a copy of original schema file in case anything goes wrong
    fso.CopyFile(dotNetSchemaFile, dotNetSchemaFile + ".bak", true);
 
    // Save the changes to the schema file
    targetXml.save(dotNetSchemaFile);
 
    WScript.Echo("Updated " + schemaFileName)
}
 
var shell = new ActiveXObject("WScript.Shell");
var vs11CommonTools = shell.ExpandEnvironmentStrings("%VS110COMNTOOLS%");
var fso = new ActiveXObject("Scripting.FileSystemObject");
 
// Check if Visual Studio is installed
if (vs11CommonTools.length == 0) {
    WScript.Echo("Could not find Visual Studio 2012 installation path");
    WScript.Quit(1);
}
 
WScript.Echo("Updating Visual Studio Schema Cache files:");
UpdateSchemaFile("DotNetConfig35.xsd");
UpdateSchemaFile("DotNetConfig40.xsd");
UpdateSchemaFile("1033\\DotNetConfig.xsd");