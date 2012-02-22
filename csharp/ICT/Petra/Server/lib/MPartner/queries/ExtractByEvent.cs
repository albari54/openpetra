//
// DO NOT REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
//
// @Authors:
//       timop
//
// Copyright 2004-2011 by OM International
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
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using Ict.Common;
using Ict.Common.DB;
using Ict.Common.Data;
using Ict.Common.Verification;
using Ict.Petra.Shared.MReporting;
using Ict.Petra.Server.MPartner.Extracts;

namespace Ict.Petra.Server.MPartner.queries
{
    /// <summary>
    /// this report is quite simple, and should be used as an example for more complex reports and extracts
    /// </summary>
    public class QueryPartnerByEvent
    {
        /// <summary>
        /// calculate an extract from a report: all partners living in a given city
        /// </summary>
        /// <param name="AParameters"></param>
        /// <param name="AResults"></param>
        /// <returns></returns>
        public static bool CalculateExtract(TParameterList AParameters, TResultList AResults)
        {
            // get the partner keys from the database
            try
            {
                Boolean ReturnValue = false;
                Boolean NewTransaction;
                TDBTransaction Transaction = DBAccess.GDBAccessObj.GetNewOrExistingTransaction(IsolationLevel.Serializable, out NewTransaction);
                bool AddressFilterAdded;

                string SqlStmt = TDataBase.ReadSqlFile("Partner.Queries.ExtractPartnerByEvent.sql");

                
                // prepare list of selected events
                List <String>param_events = new List <String>();
                foreach (TVariant choice in AParameters.Get("param_events").ToComposite())
                {
                    param_events.Add(choice.ToString());
                }
                if (param_events.Count == 0)
                {
                    throw new NoNullAllowedException("At least one option must be checked.");
                }

                // add parameters to ArrayList
                TSelfExpandingArrayList parameterList = new TSelfExpandingArrayList();

                parameterList.Add(TDbListParameterValue.OdbcListParameterValue("events", OdbcType.BigInt, param_events));
                parameterList.Add(new OdbcParameter("Accepted", OdbcType.Bit) 
                    {
                        Value = AParameters.Get("param_status_accepted").ToBool()
                    });
                parameterList.Add(new OdbcParameter("Hold", OdbcType.Bit)
                    {
                        Value = AParameters.Get("param_status_hold").ToBool()
                    });
                parameterList.Add(new OdbcParameter("Enquiry", OdbcType.Bit)
                    {
                        Value = AParameters.Get("param_status_enquiry").ToBool()
                    });
                parameterList.Add(new OdbcParameter("Cancelled", OdbcType.Bit)
                    {
                        Value = AParameters.Get("param_status_cancelled").ToBool()
                    });
                parameterList.Add(new OdbcParameter("Rejected", OdbcType.Bit)
                    {
                        Value = AParameters.Get("param_status_rejected").ToBool()
                    });
                parameterList.Add(new OdbcParameter("Active", OdbcType.Bit)
                    {
                        Value = AParameters.Get("param_active_partners").ToBool()
                    });
                parameterList.Add(new OdbcParameter("Exclude_no_soliciations", OdbcType.Bit)
                    {
                        Value = AParameters.Get("param_exclude_no_solicitations").ToBool()
                    });

                // add address filter information to sql statement and parameter list
                AddressFilterAdded = TExtractHelper.AddAddressFilter(AParameters, ref SqlStmt, ref parameterList);
                
                TLogging.Log("getting the data from the database", TLoggingType.ToStatusBar);
                DataTable partnerkeys = DBAccess.GDBAccessObj.SelectDT(SqlStmt, "partners", Transaction,
                    TExtractHelper.ConvertParameterArrayList(parameterList));

                if (NewTransaction)
                {
                    DBAccess.GDBAccessObj.RollbackTransaction();
                }

                // if this is taking a long time, every now and again update the TLogging statusbar, and check for the cancel button
                // TODO: we might need to add this functionality to TExtractsHandling.CreateExtractFromListOfPartnerKeys as well???
                if (AParameters.Get("CancelReportCalculation").ToBool() == true)
                {
                    return false;
                }

                TLogging.Log("preparing the extract", TLoggingType.ToStatusBar);

                TVerificationResultCollection VerificationResult;
                int NewExtractID;

                // create an extract with the given name in the parameters
                ReturnValue = TExtractsHandling.CreateExtractFromListOfPartnerKeys(
                    AParameters.Get("param_extract_name").ToString(),
                    AParameters.Get("param_extract_description").ToString(),
                    out NewExtractID,
                    out VerificationResult,
                    partnerkeys,
                    0,
                    AddressFilterAdded);

                if (ReturnValue)
                {
                    DBAccess.GDBAccessObj.CommitTransaction();
                }
                else
                {
                    DBAccess.GDBAccessObj.RollbackTransaction();
                }

                return ReturnValue;
            }
            catch (Exception e)
            {
                TLogging.Log(e.ToString());
                DBAccess.GDBAccessObj.RollbackTransaction();
                return false;
            }
        }
    }
}