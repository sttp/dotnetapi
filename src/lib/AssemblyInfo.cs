//******************************************************************************************************
//  AssemblyInfo.cs - Gbtc
//
//  Copyright © 2019, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the MIT License (MIT), the "License"; you may not use this
//  file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://opensource.org/licenses/MIT
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  04/12/2019 - J. Ritchie Carroll
//       Imported source code from Grid Solutions Framework.
//
//******************************************************************************************************

using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

namespace sttp
{
    /// <summary>
    /// Represents a common information provider for an assembly.
    /// </summary>
    public class AssemblyInfo
    {
        #region [ Constructors ]

        /// <summary>Initializes a new instance of the <see cref="AssemblyInfo"/> class.</summary>
        /// <param name="assemblyInstance">An <see cref="Assembly"/> object.</param>
        public AssemblyInfo(Assembly assemblyInstance)
        {
            Assembly = assemblyInstance;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the underlying <see cref="Assembly"/> being represented by this <see cref="AssemblyInfo"/> object.
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// Gets the title information of the <see cref="Assembly"/>.
        /// </summary>
        public string Title
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(AssemblyTitleAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the description information of the <see cref="Assembly"/>.
        /// </summary>
        public string Description
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(AssemblyDescriptionAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the company name information of the <see cref="Assembly"/>.
        /// </summary>
        public string Company
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(AssemblyCompanyAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the product name information of the <see cref="Assembly"/>.
        /// </summary>
        public string Product
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(AssemblyProductAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the copyright information of the <see cref="Assembly"/>.
        /// </summary>
        public string Copyright
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(AssemblyCopyrightAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the trademark information of the <see cref="Assembly"/>.
        /// </summary>
        public string Trademark
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(AssemblyTrademarkAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the configuration information of the <see cref="Assembly"/>.
        /// </summary>
        public string Configuration
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(AssemblyConfigurationAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating if the <see cref="Assembly"/> has been built as delay-signed.
        /// </summary>
        public bool DelaySign
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(AssemblyDelaySignAttribute));

                if ((object)attribute == null)
                    return false;

                return (bool)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the version information of the <see cref="Assembly"/>.
        /// </summary>
        public string InformationalVersion
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(AssemblyInformationalVersionAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the name of the file containing the key pair used to generate a strong name for the attributed <see cref="Assembly"/>.
        /// </summary>
        public string KeyFile
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(AssemblyKeyFileAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the culture name of the <see cref="Assembly"/>.
        /// </summary>
        public string CultureName
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(NeutralResourcesLanguageAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the assembly version used to instruct the System.Resources.ResourceManager to ask for a particular
        /// version of a satellite assembly to simplify updates of the main assembly of an application.
        /// </summary>
        public string SatelliteContractVersion
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(SatelliteContractVersionAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the string representing the assembly version used to indicate to a COM client that all classes
        /// in the current version of the assembly are compatible with classes in an earlier version of the assembly.
        /// </summary>
        public string ComCompatibleVersion
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(ComCompatibleVersionAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return attribute.ConstructorArguments[0].Value + "." +
                       attribute.ConstructorArguments[1].Value + "." +
                       attribute.ConstructorArguments[2].Value + "." +
                       attribute.ConstructorArguments[3].Value;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating if the <see cref="Assembly"/> is exposed to COM.
        /// </summary>
        public bool ComVisible
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(ComVisibleAttribute));

                if ((object)attribute == null)
                    return false;

                return (bool)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating if the <see cref="Assembly"/> was built in debug mode.
        /// </summary>
        public bool Debuggable
        {
            get
            {
                DebuggableAttribute attribute = Assembly.GetCustomAttributes<DebuggableAttribute>().FirstOrDefault();
                return (object)attribute != null && attribute.IsJITOptimizerDisabled;
            }
        }

        /// <summary>
        /// Gets the GUID that is used as an ID if the <see cref="Assembly"/> is exposed to COM.
        /// </summary>
        public string Guid
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(GuidAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return (string)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the string representing the <see cref="Assembly"/> version number in MajorVersion.MinorVersion format.
        /// </summary>
        public string TypeLibVersion
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(TypeLibVersionAttribute));

                if ((object)attribute == null)
                    return string.Empty;

                return attribute.ConstructorArguments[0].Value + "." +
                       attribute.ConstructorArguments[1].Value;
            }
        }

        /// <summary>
        /// Gets a boolean value indicating whether the <see cref="Assembly"/> is CLS-compliant.
        /// </summary>
        public bool CLSCompliant
        {
            get
            {
                CustomAttributeData attribute = GetCustomAttribute(typeof(CLSCompliantAttribute));

                if ((object)attribute == null)
                    return false;

                return (bool)attribute.ConstructorArguments[0].Value;
            }
        }

        /// <summary>
        /// Gets the path or UNC location of the loaded file that contains the manifest.
        /// </summary>
        public string Location => Assembly.Location;

        /// <summary>
        /// Gets the location of the <see cref="Assembly"/> as specified originally.
        /// </summary>
        public string CodeBase => Assembly.CodeBase.Replace("file:///", "");

        /// <summary>
        /// Gets the display name of the <see cref="Assembly"/>.
        /// </summary>
        public string FullName => Assembly.FullName;

        /// <summary>
        /// Gets the simple, unencrypted name of the <see cref="Assembly"/>.
        /// </summary>
        public string Name => Assembly.GetName().Name;

        /// <summary>
        /// Gets the major, minor, revision, and build numbers of the <see cref="Assembly"/>.
        /// </summary>
        public Version Version => Assembly.GetName().Version;

        /// <summary>
        /// Gets the string representing the version of the common language runtime (CLR) saved in the file
        /// containing the manifest.
        /// </summary>
        public string ImageRuntimeVersion => Assembly.ImageRuntimeVersion;

        /// <summary>
        /// Gets a boolean value indicating whether the <see cref="Assembly"/> was loaded from the global assembly cache.
        /// </summary>
        public bool GACLoaded => Assembly.GlobalAssemblyCache;

        /// <summary>
        /// Gets the date and time when the <see cref="Assembly"/> was built.
        /// </summary>
        public DateTime BuildDate => File.GetLastWriteTime(Assembly.Location);

        /// <summary>
        /// Gets the root namespace of the <see cref="Assembly"/>.
        /// </summary>
        public string RootNamespace => Assembly.GetExportedTypes()[0].Namespace;

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Gets a collection of assembly attributes exposed by the assembly.
        /// </summary>
        /// <returns>A System.Specialized.KeyValueCollection of assembly attributes.</returns>
        public NameValueCollection GetAttributes()
        {
            NameValueCollection assemblyAttributes = new NameValueCollection();

            //Add some values that are not in AssemblyInfo.
            assemblyAttributes.Add("Full Name", FullName);
            assemblyAttributes.Add("Name", Name);
            assemblyAttributes.Add("Version", Version.ToString());
            assemblyAttributes.Add("Image Runtime Version", ImageRuntimeVersion);
            assemblyAttributes.Add("Build Date", BuildDate.ToString());
            assemblyAttributes.Add("Location", Location);
            assemblyAttributes.Add("Code Base", CodeBase);
            assemblyAttributes.Add("GAC Loaded", GACLoaded.ToString());

            //Add all attributes available from AssemblyInfo.
            assemblyAttributes.Add("Title", Title);
            assemblyAttributes.Add("Description", Description);
            assemblyAttributes.Add("Company", Company);
            assemblyAttributes.Add("Product", Product);
            assemblyAttributes.Add("Copyright", Copyright);
            assemblyAttributes.Add("Trademark", Trademark);
            assemblyAttributes.Add("Configuration", Configuration);
            assemblyAttributes.Add("Delay Sign", DelaySign.ToString());
            assemblyAttributes.Add("Informational Version", InformationalVersion);
            assemblyAttributes.Add("Key File", KeyFile);
            assemblyAttributes.Add("Culture Name", CultureName);
            assemblyAttributes.Add("Satellite Contract Version", SatelliteContractVersion);
            assemblyAttributes.Add("Com Compatible Version", ComCompatibleVersion);
            assemblyAttributes.Add("Com Visible", ComVisible.ToString());
            assemblyAttributes.Add("Guid", Guid);
            assemblyAttributes.Add("Type Lib Version", TypeLibVersion);
            assemblyAttributes.Add("CLS Compliant", CLSCompliant.ToString());

            return assemblyAttributes;
        }

        /// <summary>
        /// Gets the specified assembly attribute if it is exposed by the assembly.
        /// </summary>
        /// <param name="attributeType">Type of the attribute to get.</param>
        /// <returns>The requested assembly attribute if it exists; otherwise null.</returns>
        /// <remarks>
        /// This method always returns <c>null</c> under Mono deployments.
        /// </remarks>
        public CustomAttributeData GetCustomAttribute(Type attributeType)
        {
#if MONO
            // TODO: Validate that these functions still do not work under Mono
            return null;
#else
            //Returns the requested assembly attribute.
            return Assembly.GetCustomAttributesData().FirstOrDefault(assemblyAttribute => assemblyAttribute.Constructor.DeclaringType == attributeType);
#endif
        }

        /// <summary>
        /// Gets the specified embedded resource from the assembly.
        /// </summary>
        /// <param name="resourceName">The full name (including the namespace) of the embedded resource to get.</param>
        /// <returns>The embedded resource.</returns>
        public Stream GetEmbeddedResource(string resourceName) => Assembly.GetManifestResourceStream(resourceName);

        #endregion

        #region [ Static ]

        // Static Fields
        private static AssemblyInfo s_entryAssembly;
        private static AssemblyInfo s_executingAssembly;

        /// <summary>
        /// Gets the <see cref="AssemblyInfo"/> object of the process executable in the default application domain.
        /// </summary>
        public static AssemblyInfo EntryAssembly
        {
            get
            {
                if ((object)s_entryAssembly == null)
                {
                    Assembly entryAssembly = Assembly.GetEntryAssembly();

                    if ((object)entryAssembly == null)
                        entryAssembly = Assembly.ReflectionOnlyLoadFrom(Process.GetCurrentProcess().MainModule.FileName);

                    s_entryAssembly = new AssemblyInfo(entryAssembly);
                }

                return s_entryAssembly;
            }
        }

        /// <summary>
        /// Gets the <see cref="AssemblyInfo"/> object of the assembly that contains the code that is currently executing.
        /// </summary>
        public static AssemblyInfo ExecutingAssembly
        {
            get
            {
                // Caller's assembly will be the executing assembly for the caller
                if ((object)s_executingAssembly == null)
                    s_executingAssembly = new AssemblyInfo(Assembly.GetCallingAssembly());

                return s_executingAssembly;
            }
        }

        #endregion
    }
}