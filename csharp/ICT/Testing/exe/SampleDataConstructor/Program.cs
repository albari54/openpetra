//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       thomass, timop
//
// Copyright 2004-2012 by OM International
//
// This file is part of OpenPetra.org.
//
// OpenPetra.org is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// OpenPetra.org is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with OpenPetra.org.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;

using Ict.Testing.NUnitPetraServer;
using Ict.Petra.Shared.MCommon.Data;
using Ict.Petra.Shared.MPartner;
using Ict.Petra.Shared.MPartner.Partner.Data;
using Ict.Petra.Shared.Interfaces.MSysMan.ImportExport.WebConnectors;
using Ict.Petra.Server.MSysMan.ImportExport.WebConnectors;
using Ict.Petra.Client.App.Core;
using Ict.Petra.Client.App.Core.RemoteObjects;
using Ict.Petra.Server.App.Core;
using Ict.Common.Remoting.Server;
using Ict.Common.Remoting.Shared;
using Ict.Common.Verification;
using Ict.Common;

namespace Ict.Testing.SampleDataConstructor
{
    /// <summary>
    /// This class creates sample data (partners, organisations, gifts) and imports them into OpenPetra.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The class requires raw data to have been created already by benerator, reads this data, enhances
    /// and compiles it (using the literal meaning of "compile", i.e. putting together People, Addresses,
    /// Phonenumbers to create partners), and them imports this data to the OpenPetra Server.
    /// </para>
    /// <para>
    /// Generally, the Sample Data creator DOES NOT use the Petra Model internally,
    /// although it tries to stay close to it ( e.g. Naming Convention).
    /// This is so it can run a simple simulation for creating events (marriages resulting in same location, children, gift entries).
    /// These can then be saved in Petra.
    /// </para>
    ///
    /// TODO: Check comment from Timo: This is actually rather a tool than a test
    /// - so one could change it's location.
    /// </remarks>
    class TSampleDataConstructor
    {
        /// <summary>
        /// Creates Sample Data using the raw data provided and exports this to the Petra Server
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            TLogging.Log("Running Sample Data Constructor");

            try
            {
                TLogging.Log("(1) Initialize (check availability of resources, start the server)");

                TLogging.Log("\tStarting the server...");

                // use the config file defined on the command line with -C:
                TPetraServerConnector.Connect(string.Empty);

                // data directory containing the raw data files created by benerator
                string datadirectory = TAppSettingsManager.GetValue("dir.data.generated");

                string operation = TAppSettingsManager.GetValue("operation", "importPartners");

                if ((operation == "importPartners") || (operation == "all"))
                {
                    TLogging.Log("(2) Import partners");
                    SampleDataBankPartners.GenerateBanks(
                        Path.Combine(datadirectory, "banks.csv"));

                    SampleDataDonors.GenerateFamilyPartners(
                        Path.Combine(datadirectory, "people.csv"));

                    TLogging.Log("(3) Import organisations");
                    SampleDataOrganisations.GenerateOrganisationPartners(
                        Path.Combine(datadirectory, "organisations.csv"));
                }

                TLogging.Log("(4) Import recipients");

                operation = TAppSettingsManager.GetValue("operation", "importRecipients");

                if ((operation == "importRecipients") || (operation == "all"))
                {
                    // parse random data generated by benerator
                    SampleDataUnitPartners.GenerateFields(
                        Path.Combine(datadirectory, "fields.csv"));
                    SampleDataUnitPartners.GenerateKeyMinistries(
                        Path.Combine(datadirectory, "keymins.csv"));
                    SampleDataWorkers.GenerateWorkers(
                        Path.Combine(datadirectory, "workers.csv"));
                }

                // TODO change number of forwarding periods to 12

                TLogging.Log("(5) Create gift batches");

                operation = TAppSettingsManager.GetValue("operation", "importDonations");

                if ((operation == "importDonations") || (operation == "all"))
                {
                    // parse random data generated by benerator
                    SampleDataGiftBatches.GenerateBatches(Path.Combine(datadirectory, "donations.csv"));
                }

                TLogging.Log("(6) Create invoices");

                if ((operation == "importInvoices") || (operation == "all"))
                {
                    // parse random data generated by benerator
                    SampleDataAccountsPayable.GenerateInvoices(Path.Combine(datadirectory, "invoices.csv"));
                }

                TLogging.Log("(7) Post gift batches");

                operation = TAppSettingsManager.GetValue("operation", "postDonations");

                if ((operation == "postDonations") || (operation == "all"))
                {
                    for (int periodCounter = 1; periodCounter < 6; periodCounter++)
                    {
                        TLogging.Log("posting gift batches of period " + periodCounter.ToString());

                        if (!SampleDataGiftBatches.PostBatches(0, periodCounter))
                        {
                            throw new Exception("failed to post gift batch");
                        }
                    }

                    TLogging.Log("posting gift batches of period 6");

                    if (!SampleDataGiftBatches.PostBatches(0, 6, 1))
                    {
                        throw new Exception("failed to post gift batch");
                    }
                }

                operation = TAppSettingsManager.GetValue("operation", "exportGifts");

                if ((operation == "exportGifts") || (operation == "all"))
                {
                    SampleDataBankImportFiles.ExportGiftBatches(datadirectory);
                }

                TLogging.Log("(8) Posting and paying invoices");

                operation = TAppSettingsManager.GetValue("operation", "postInvoices");

                if ((operation == "postInvoices") || (operation == "all"))
                {
                    for (int periodCounter = 1; periodCounter < 6; periodCounter++)
                    {
                        TLogging.Log("posting invoices of period " + periodCounter.ToString());
                        SampleDataAccountsPayable.PostAndPayInvoices(0, periodCounter);
                    }

                    TLogging.Log("posting invoices of period 6");
                    SampleDataAccountsPayable.PostAndPayInvoices(0, 6, 1);
                }

                TLogging.Log("(9) Creating applications for conference");

                operation = TAppSettingsManager.GetValue("operation", "conferenceApplications");

                if (operation == "conferenceApplications")
                {
                    SampleDataConferenceApplicants.GenerateApplications(Path.Combine(datadirectory, "conferenceApplications.csv"));
                }
                else
                {
                    TLogging.Log("Please explicitely run nant importDemodata -D:operation=conferenceApplications");
                }

                TLogging.Log("Completed.");
            }
            catch (Exception e)
            {
                TLogging.Log(e.Message);
                TLogging.Log(e.StackTrace);
                Environment.Exit(-1);
            }
        }
    }
}