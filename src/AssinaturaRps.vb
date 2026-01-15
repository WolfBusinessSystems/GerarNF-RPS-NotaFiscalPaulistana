Option Strict On

Imports System.Globalization
Imports System.Security.Cryptography
Imports System.Security.Cryptography.X509Certificates
Imports System.Text

Public NotInheritable Class AssinaturaRps
    Private Sub New()
    End Sub

    Public Shared Function MontarCadeia(dados As RpsDados, prestador As Prestador, tomador As Tomador) As String
        ' Etapa 1: validar entradas obrigatorias.
        If dados Is Nothing Then
            Throw New ArgumentNullException(NameOf(dados))
        End If
        If prestador Is Nothing Then
            Throw New ArgumentNullException(NameOf(prestador))
        End If
        If tomador Is Nothing Then
            Throw New ArgumentNullException(NameOf(tomador))
        End If

        ' Etapa 2: formatar cada campo conforme o layout de 86 posicoes.
        Dim im = SomenteNumeros(prestador.InscricaoMunicipal).PadLeft(8, "0"c)
        Dim serie = AjustarSerie(dados.Serie)
        Dim numero = dados.Numero.ToString(CultureInfo.InvariantCulture).PadLeft(12, "0"c)
        Dim dataEmissao = dados.DataEmissao.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
        Dim tributacao = AjustarUmChar(dados.Tributacao, "T"c)
        Dim status = AjustarUmChar(dados.Status, "N"c)
        Dim issRetido = NormalizarIssRetidoChar(dados.IssRetido)
        Dim valorServicos = FormatarValor(dados.ValorServicos)
        Dim valorDeducoes = FormatarValor(dados.ValorDeducoes)
        Dim codigoServico = SomenteNumeros(dados.CodigoServico).PadLeft(5, "0"c)
        Dim indTomador = AjustarUmChar(dados.IndTomador, "1"c)
        Dim docTomador = SomenteNumeros(tomador.Documento).PadLeft(14, "0"c)

        ' Etapa 3: montar a cadeia ASCII na ordem exigida pelo manual.
        Dim cadeia = im & serie & numero & dataEmissao & tributacao & status & issRetido &
            valorServicos & valorDeducoes & codigoServico & indTomador & docTomador

        If cadeia.Length <> 86 Then
            Throw New InvalidOperationException("Cadeia de assinatura com tamanho invalido: " & cadeia.Length.ToString(CultureInfo.InvariantCulture))
        End If

        Return cadeia
    End Function

    Public Shared Function AssinarCadeia(cadeia As String, certificado As X509Certificate2) As String
        ' Etapa 1: validar inputs e garantir chave privada no certificado.
        If String.IsNullOrWhiteSpace(cadeia) Then
            Throw New ArgumentException("Cadeia obrigatoria.", NameOf(cadeia))
        End If
        If certificado Is Nothing Then
            Throw New ArgumentNullException(NameOf(certificado))
        End If
        If Not certificado.HasPrivateKey Then
            Throw New InvalidOperationException("Certificado sem chave privada.")
        End If

        ' Etapa 2: converter a cadeia para ASCII (obrigatorio pelo layout).
        Dim bytes = Encoding.ASCII.GetBytes(cadeia)
        Dim rsa = certificado.GetRSAPrivateKey()
        If rsa Is Nothing Then
            Throw New InvalidOperationException("Chave RSA nao encontrada no certificado.")
        End If

        ' Etapa 3: assinar o byte array usando RSA-SHA1 (sem assinar hash de hash).
        Dim assinatura As Byte()
        Dim rsaCsp = TryCast(rsa, RSACryptoServiceProvider)
        If rsaCsp IsNot Nothing Then
            assinatura = rsaCsp.SignData(bytes, New SHA1CryptoServiceProvider())
        Else
            assinatura = rsa.SignData(bytes, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1)
        End If

        ' Etapa 4: retornar Base64 pronto para o campo Assinatura do RPS.
        Return Convert.ToBase64String(assinatura)
    End Function

    Private Shared Function AjustarSerie(serie As String) As String
        ' Serie precisa ter 5 posicoes, preenchendo com espacos a direita.
        Dim valor = If(serie, String.Empty)
        If valor.Length > 5 Then
            valor = valor.Substring(0, 5)
        End If
        Return valor.PadRight(5, " "c)
    End Function

    Private Shared Function AjustarUmChar(valor As String, padrao As Char) As String
        ' Normaliza para um unico caractere.
        Dim texto = If(valor, String.Empty).Trim().ToUpperInvariant()
        If texto.Length = 0 Then
            Return padrao.ToString()
        End If
        Return texto.Substring(0, 1)
    End Function

    Private Shared Function NormalizarIssRetidoChar(valor As String) As String
        ' Converte ISSRetido para o formato 1 char usado na cadeia (S/N).
        Dim texto = If(valor, String.Empty).Trim().ToLowerInvariant()
        If texto = "s" OrElse texto = "1" OrElse texto = "true" Then
            Return "S"
        End If
        Return "N"
    End Function

    Private Shared Function FormatarValor(valor As Decimal) As String
        ' Valor com 2 decimais, sem separador, padding a esquerda ate 15.
        Dim arredondado = Decimal.Round(valor, 2, MidpointRounding.AwayFromZero)
        Dim semSeparador = (arredondado * 100D).ToString("0", CultureInfo.InvariantCulture)
        Return semSeparador.PadLeft(15, "0"c)
    End Function

    Private Shared Function SomenteNumeros(valor As String) As String
        ' Remove todos os caracteres nao numericos.
        If String.IsNullOrWhiteSpace(valor) Then
            Return String.Empty
        End If

        Dim sb = New StringBuilder(valor.Length)
        For Each ch As Char In valor
            If Char.IsDigit(ch) Then
                sb.Append(ch)
            End If
        Next
        Return sb.ToString()
    End Function
End Class
