Public Class myStr
    Private e_str As String
    Public Event VariableChanged(ByVal e_str As String)
    Public Property Variable() As String
        Get
            Variable = e_str
        End Get
        Set(ByVal value As String)
            e_str = value
            RaiseEvent VariableChanged(e_str)
        End Set
    End Property
End Class
