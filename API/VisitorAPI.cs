﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace API
{
    public class VisitorAPI
    {
        [Function("addvisitor")]
        public static async Task<HttpResponseData> AddVisitor([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData incomingRequest)
        {
            try
            {
                //get new visitor data out of incoming request 
                var newVisitorXml = await APITools.ExtractDataFromRequest(incomingRequest);

                //add new visitor to main list
                await APITools.AddXElementToXDocumentAzure(newVisitorXml, APITools.VisitorLogFile, APITools.BlobContainerName);

                return APITools.PassMessage(incomingRequest);
            }
            catch (Exception e)
            {
                //log error
                await APILogger.Error(e, incomingRequest);

                //format error nicely to show user
                return APITools.FailMessage(e, incomingRequest);
            }

        }

        /// <summary>
        /// Gets all the unique visitors to the site
        /// </summary>
        [Function("getvisitorlist")]
        public static async Task<HttpResponseData> GetVisitorList([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData incomingRequest)
        {

            try
            {

                //get user id
                //var userId = APITools.ExtractDataFromRequest(incomingRequest).Value;

                //get visitor log from storage
                var visitorLogXml = await APITools.GetXmlFileFromAzureStorage(APITools.VisitorLogFile, APITools.BlobContainerName);

                //get all unique visitor elements only
                //var uniqueVisitorList = from visitorXml in visitorLogXml.Root?.Elements()
                //                        where
                //                            //note: location tag only exists for new visitor log,
                //                            //so use that to get unique list
                //                            visitorXml.Element("Location") != null
                //                        select visitorXml;

                //convert list to nice string before sending to caller
                var visitorLogXmlString = visitorLogXml?.Root?.ToString(SaveOptions.DisableFormatting) ?? "<Empty/>";
                return APITools.PassMessage(visitorLogXmlString, incomingRequest);

            }
            catch (Exception e)
            {
                //log error
                await APILogger.Error(e, incomingRequest);

                //format error nicely to show user
                return APITools.FailMessage(e, incomingRequest);
            }
        }

        [Function("deletevisitorbyuserid")]
        public static async Task<HttpResponseData> DeleteVisitorByUserId([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData incomingRequest)
        {

            try
            {
                //get unedited hash & updated person details from incoming request
                var userIdXml = await APITools.ExtractDataFromRequest(incomingRequest);
                var userId = userIdXml.Value;

                //get all visitor elements that needs to be deleted
                var visitorLogClient = await APITools.GetBlobClientAzure(APITools.VisitorLogFile, APITools.BlobContainerName);
                var visitorListXml = await APITools.BlobClientToXmlDoc(visitorLogClient);
                var visitorLogsToDelete = visitorListXml.Root?.Elements().Where(x => x.Element("UserId")?.Value == userId).ToList();

                //delete each record
                foreach (var visitorXml in visitorLogsToDelete)
                {
                    visitorXml.Remove();
                }

                //upload modified list to storage
                await APITools.OverwriteBlobData(visitorLogClient, visitorListXml);

                return APITools.PassMessage(incomingRequest);

            }
            catch (Exception e)
            {
                //log error
                await APILogger.Error(e, incomingRequest);
                //format error nicely to show user
                return APITools.FailMessage(e, incomingRequest);
            }

        }

        [Function("deletevisitorbyvisitorid")]
        public static async Task<HttpResponseData> DeleteVisitorByVisitorId([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequestData incomingRequest)
        {

            try
            {
                //get unedited hash & updated person details from incoming request
                var visitorIdXml = await APITools.ExtractDataFromRequest(incomingRequest);
                var visitorId = visitorIdXml.Value;

                //get all visitor elements that needs to be deleted
                var visitorLogClient = await APITools.GetBlobClientAzure(APITools.VisitorLogFile, APITools.BlobContainerName);
                var visitorListXml = await APITools.BlobClientToXmlDoc(visitorLogClient);
                var visitorLogsToDelete = (from xml in visitorListXml.Root?.Elements()
                                           where xml.Element("VisitorId")?.Value == visitorId
                                           select xml).ToList();

                //delete each record
                foreach (var visitorXml in visitorLogsToDelete)
                {
                    visitorXml.Remove();
                }

                //upload modified list to storage
                await APITools.OverwriteBlobData(visitorLogClient, visitorListXml);

                return APITools.PassMessage(incomingRequest);

            }
            catch (Exception e)
            {
                //log error
                await APILogger.Error(e, incomingRequest);
                //format error nicely to show user
                return APITools.FailMessage(e, incomingRequest);
            }

        }


    }
}
