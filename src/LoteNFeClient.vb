Option Strict On

Imports System.Web.Services
Imports System.Web.Services.Protocols

<WebServiceBinding(Name:="LoteNFeSoap", [Namespace]:="http://www.prefeitura.sp.gov.br/nfe")>
Public Class LoteNFeClient
    Inherits SoapHttpClientProtocol

    Public Sub New(endpointUrl As String)
        ' Etapa 1: configurar URL do Web Service.
        If String.IsNullOrWhiteSpace(endpointUrl) Then
            Throw New ArgumentException("Endpoint obrigatorio.", NameOf(endpointUrl))
        End If
        Me.Url = endpointUrl
    End Sub

    ' Metodo sincrono conforme WSDL: EnvioRPS(VersaoSchema, MensagemXML).
    <SoapDocumentMethod("http://www.prefeitura.sp.gov.br/nfe/EnvioRPS",
        RequestNamespace:="http://www.prefeitura.sp.gov.br/nfe",
        ResponseNamespace:="http://www.prefeitura.sp.gov.br/nfe",
        Use:=SoapBindingUse.Literal,
        ParameterStyle:=SoapParameterStyle.Wrapped)>
    Public Function EnvioRPS(versaoSchema As Integer, mensagemXML As String) As String
        Dim results = Me.Invoke("EnvioRPS", New Object() {versaoSchema, mensagemXML})
        Return CType(results(0), String)
    End Function
End Class
