Option Strict On

Imports System.Net
Imports System.Security.Cryptography.X509Certificates

Public Class EnvioRpsService
    Private ReadOnly _endpointUrl As String

    Public Sub New(endpointUrl As String)
        ' Endpoint do Web Service (homologacao ou producao).
        If String.IsNullOrWhiteSpace(endpointUrl) Then
            Throw New ArgumentException("Endpoint obrigatorio.", NameOf(endpointUrl))
        End If
        _endpointUrl = endpointUrl
    End Sub

    Public Function EnviarRps(
        dados As RpsDados,
        prestador As Prestador,
        tomador As Tomador,
        config As EnvioConfig,
        certificado As X509Certificate2
    ) As EnvioRpsResultado
        ' Etapa 1: garantir TLS 1.2 para o envio do SOAP.
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12

        ' Etapa 2: montar a cadeia de 86 posicoes e assinar com RSA-SHA1.
        Dim cadeia = AssinaturaRps.MontarCadeia(dados, prestador, tomador)
        Dim assinaturaRps = AssinaturaRps.AssinarCadeia(cadeia, certificado)

        ' Etapa 3: montar o XML do PedidoEnvioRPS com os dados do RPS.
        Dim xmlDoc = RpsXmlBuilder.MontarPedidoEnvioRpsXml(dados, prestador, tomador, config, assinaturaRps)

        ' Etapa 4: assinar o XML completo (Signature no elemento raiz).
        ' Caso o schema exija assinatura por RPS, use AssinaturaXml.AssinarRps.
        AssinaturaXml.AssinarPedidoEnvio(xmlDoc, certificado)

        ' Etapa 5: enviar o XML assinado para o servico SOAP.
        Dim xmlAssinado = xmlDoc.OuterXml

        Dim cliente = New LoteNFeClient(_endpointUrl)
        ' Certificado do emissor/transmissor usado no handshake TLS.
        cliente.ClientCertificates.Add(certificado)

        ' Etapa 6: chamar EnvioRPS (VersaoSchema, MensagemXML) e interpretar retorno.
        Dim resposta = cliente.EnvioRPS(config.VersaoSchema, xmlAssinado)
        Return RespostaEnvioRpsParser.Parse(resposta)
    End Function
End Class
