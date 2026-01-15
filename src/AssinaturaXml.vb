Option Strict On

Imports System.Security.Cryptography
Imports System.Security.Cryptography.Xml
Imports System.Security.Cryptography.X509Certificates
Imports System.Xml

Public NotInheritable Class AssinaturaXml
    Private Sub New()
    End Sub

    ' Assina o XML completo do PedidoEnvioRPS e adiciona <Signature /> no elemento raiz.
    Public Shared Sub AssinarPedidoEnvio(xml As XmlDocument, certificado As X509Certificate2)
        ' Etapa 1: assinar o documento inteiro (Reference.Uri vazio).
        If xml Is Nothing OrElse xml.DocumentElement Is Nothing Then
            Throw New InvalidOperationException("Documento XML sem elemento raiz.")
        End If
        Assinar(xml, xml.DocumentElement, String.Empty, certificado)
    End Sub

    ' Assina apenas o elemento RPS (uso alternativo caso o layout exija assinatura por Id).
    Public Shared Sub AssinarRps(xml As XmlDocument, rpsId As String, certificado As X509Certificate2)
        If String.IsNullOrWhiteSpace(rpsId) Then
            Throw New ArgumentException("Id do RPS obrigatorio.", NameOf(rpsId))
        End If

        ' Etapa 1: localizar o RPS pelo atributo Id e assinar o elemento.
        Dim elemento = TryCast(xml.SelectSingleNode("//*[@Id='" & rpsId & "']"), XmlElement)
        If elemento Is Nothing Then
            Throw New InvalidOperationException("Elemento RPS nao encontrado para assinatura.")
        End If

        Assinar(xml, elemento, "#" & rpsId, certificado)
    End Sub

    Private Shared Sub Assinar(xml As XmlDocument, elementoAssinado As XmlElement, referenciaUri As String, certificado As X509Certificate2)
        If xml Is Nothing Then
            Throw New ArgumentNullException(NameOf(xml))
        End If
        If elementoAssinado Is Nothing Then
            Throw New ArgumentNullException(NameOf(elementoAssinado))
        End If
        If certificado Is Nothing Then
            Throw New ArgumentNullException(NameOf(certificado))
        End If
        If Not certificado.HasPrivateKey Then
            Throw New InvalidOperationException("Certificado sem chave privada.")
        End If

        ' Etapa 2: obter a chave RSA do certificado A1.
        Dim rsa = certificado.GetRSAPrivateKey()
        If rsa Is Nothing Then
            Throw New InvalidOperationException("Chave RSA nao encontrada no certificado.")
        End If

        ' Etapa 3: configurar a assinatura XMLDSIG enveloped + C14N + SHA1.
        Dim signedXml = New SignedXml(xml)
        signedXml.SigningKey = rsa

        Dim reference = New Reference()
        reference.Uri = referenciaUri
        reference.DigestMethod = SignedXml.XmlDsigSHA1Url
        reference.AddTransform(New XmlDsigEnvelopedSignatureTransform())
        reference.AddTransform(New XmlDsigC14NTransform())

        signedXml.AddReference(reference)
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl
        signedXml.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url

        ' Etapa 4: anexar o certificado do usuario final (EndCertOnly).
        Dim keyInfo = New KeyInfo()
        Dim x509Data = New KeyInfoX509Data(certificado)
        x509Data.AddSubjectName(certificado.Subject)
        keyInfo.AddClause(x509Data)
        signedXml.KeyInfo = keyInfo

        ' Etapa 5: gerar a assinatura e anexar ao XML.
        signedXml.ComputeSignature()

        Dim assinatura = signedXml.GetXml()
        elementoAssinado.AppendChild(xml.ImportNode(assinatura, True))
    End Sub
End Class
