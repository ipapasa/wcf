﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading.Tasks;
using Infrastructure.Common;
using Xunit;

public static class OperationBehaviorTest
{
    [WcfFact]
    public static void IOperationBehavior_Methods_AreCalled()
    {
        DuplexClientBase<ICustomOperationBehaviorDuplexService> duplexService = null;
        ICustomOperationBehaviorDuplexService proxy = null;

        NetTcpBinding binding = new NetTcpBinding();
        binding.Security.Mode = SecurityMode.None;

        WcfDuplexServiceCallback callbackService = new WcfDuplexServiceCallback();
        InstanceContext context = new InstanceContext(callbackService);

        duplexService = new MyDuplexClientBase<ICustomOperationBehaviorDuplexService>(context, binding, new EndpointAddress(FakeAddress.TcpAddress));
        proxy = duplexService.ChannelFactory.CreateChannel();

        // Wait to validate until the process has been given a reasonable time to complete.
        Task[] taskCollection = { MyOperationBehavior.validateMethodTcs.Task, MyOperationBehavior.addBindingParametersMethodTcs.Task, MyOperationBehavior.applyClientBehaviorMethodTcs.Task };
        bool waitAll = Task.WaitAll(taskCollection, 250);

        Assert.True(MyOperationBehavior.errorBuilder.Length == 0, "Test case FAILED with errors: " + MyOperationBehavior.errorBuilder.ToString());
        Assert.True(waitAll, "None of the IOperationBehavior methods were called.");

        ((ICommunicationObject)proxy).Close();
        ((ICommunicationObject)duplexService).Close();
    }

    [WcfFact]
    // Validate that we can use XmlSerializerOperationBehavior to modify or add XmlSerializerFormatAttribute on interface operations.
    public static void XmlSerializerOperationBehavior_BasicUsage()
    {
        XmlSerializerOperationBehavior serializerBehavior;
        BasicHttpBinding binding = new BasicHttpBinding();
        string baseAddress = "http://localhost:1066/SomeService";
        ChannelFactory<IXmlTestingType> factory = new ChannelFactory<IXmlTestingType>(binding, new EndpointAddress(baseAddress));
        ContractDescription cd = factory.Endpoint.Contract;
        OperationDescriptionCollection collection = cd.Operations;

        foreach (OperationDescription description in collection)
        {
            // Find the serializer behavior for those operations that have the attribute set via the interface.
            serializerBehavior = description.Behaviors.Find<XmlSerializerOperationBehavior>();
            if (serializerBehavior == null)
            {
                // This operation was not set with XmlSerializerFormatAttribute
                // Here we add the attribute programatically using defaults.
                if (String.Equals(description.Name, nameof(IXmlTestingType.XmlSerializerFormatAttribute_NotSet_One)))
                {
                    // Default OperationFormatStyle is "Document"
                    serializerBehavior = new XmlSerializerOperationBehavior(description);
                    description.Behaviors.Add(serializerBehavior);
                }
                // There is one additional operation not set with XmlSerializerFormatAttribute
                // Here we add the attribute programatically and further set the OperationFormatStyle to 'Rpc'
                else
                {
                    XmlSerializerFormatAttribute serializerAttribute = new XmlSerializerFormatAttribute();
                    serializerAttribute.Style = OperationFormatStyle.Rpc;

                    serializerBehavior = new XmlSerializerOperationBehavior(description, serializerAttribute);
                    description.Behaviors.Add(serializerBehavior);
                }

            }

            if (String.Equals(description.Name, nameof(IXmlTestingType.XmlSerializerFormatAttribute_Set_StyleSetTo_Rpc)) || (String.Equals(description.Name, nameof(IXmlTestingType.XmlSerializerFormatAttribute_NotSet_Two))))
            {
                Assert.True(String.Equals(serializerBehavior.XmlSerializerFormatAttribute.Style.ToString(), "Rpc"));
            }
            else
            {
                Assert.True(String.Equals(serializerBehavior.XmlSerializerFormatAttribute.Style.ToString(), "Document"));
            }
        }
    }

    [ServiceContract]
    public interface IXmlTestingType
    {
        [OperationContract]
        [XmlSerializerFormat(Style = OperationFormatStyle.Rpc)]
        void XmlSerializerFormatAttribute_Set_StyleSetTo_Rpc();

        [OperationContract]
        void XmlSerializerFormatAttribute_NotSet_One();
        [OperationContract]
        void XmlSerializerFormatAttribute_NotSet_Two();

        [OperationContract]
        [XmlSerializerFormat]
        void XmlSerializerFormatAttribute_Set_StyleSetTo_Default();

        [OperationContract]
        [XmlSerializerFormat(Style = OperationFormatStyle.Document)]
        void XmlSerializerFormatAttribute_Set_StyleSetTo_Document();
    }
}
