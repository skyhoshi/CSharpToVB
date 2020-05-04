﻿Imports Xunit

Namespace MSCoreReference.Tests
    Public NotInheritable Class ProgressReportTests

        <Fact>
        Public Shared Sub ProgressReportConstructor()
            Dim p As New ProgressReport(1, 10)
            Assert.Equal(p.Current, 1)
            Assert.Equal(p.Maximum, 10)
            Assert.True(p.Equals(p))
            Assert.True(p = p)
            Assert.False(p.Equals(New ProgressReport(1, 11)))
            Assert.True(p <> New ProgressReport(1, 11))
        End Sub
    End Class
End Namespace
