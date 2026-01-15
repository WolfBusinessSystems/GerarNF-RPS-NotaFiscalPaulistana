Option Strict On

Imports System.Security.Cryptography.X509Certificates

Public NotInheritable Class CertificadoA1
    Private Sub New()
    End Sub

    Public Shared Function CarregarDoArquivo(caminhoPfx As String, senha As String) As X509Certificate2
        ' Etapa 1: validar caminho e carregar o certificado A1 do arquivo PFX.
        If String.IsNullOrWhiteSpace(caminhoPfx) Then
            Throw New ArgumentException("Caminho do PFX obrigatorio.", NameOf(caminhoPfx))
        End If

        ' Etapa 2: usar flags que garantem chave privada e compatibilidade no Windows.
        Dim flags = X509KeyStorageFlags.MachineKeySet Or X509KeyStorageFlags.PersistKeySet Or X509KeyStorageFlags.Exportable
        Return New X509Certificate2(caminhoPfx, senha, flags)
    End Function
End Class
