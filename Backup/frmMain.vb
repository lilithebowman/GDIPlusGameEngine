Imports System.Drawing
Imports System.Threading

Class frmMain
    Inherits Form



    ' some vars containing properties:
    Dim DisplaySize As New Size(800, 600)
    Dim Antialiasing As Boolean = True

    ' We raise our own paint event. We only need one argument in this example.
    Shadows Event Paint(ByVal G As Graphics)

    ' some other stuff
    Private _counter As Long = 0
    Private _Message As String = "Hello World!"
    Private _Font = New Font(Me.Font.Name, 32, FontStyle.Bold)

    ' timer for a simple FPS counter
    Private WithEvents _fpstimer As New Windows.Forms.Timer
    Private _fps As Integer = 0, _fpsstep As Integer = 0

    Sub New()

        ' This call is required by the Windows Form Designer.
        InitializeComponent()

        ' timer
        _fpstimer.Interval = 1000
        _fpstimer.Start()

        ' Add any initialization after the InitializeComponent() call.
 
        ' configure form:
        Me.SetStyle(System.Windows.Forms.ControlStyles.AllPaintingInWmPaint, True) ' True is better
        Me.SetStyle(System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer, True) ' True is better

        ' Disables the on built PAINT event. We dont need it with a renderloop.
        ' we will raise the paint event ourselves
        Me.SetStyle(System.Windows.Forms.ControlStyles.UserPaint, False) ' False is better

        ' We want our drawing area to be the desired DisplaySizesize
        ' - form will grow to accommodate client area.
        Me.ClientSize = Me.DisplaySize

        ' Lock windowsize
        Me.FormBorderStyle = Windows.Forms.FormBorderStyle.FixedSingle

        ' Resize events are ignored:
        Me.SetStyle(ControlStyles.FixedHeight, True)
        Me.SetStyle(ControlStyles.FixedWidth, True)



    End Sub

    Private Sub frmMain_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        ' I am starting the thread here after all formloading is complete.
        ' Lets create a thread the easy way. Fire and Forget.
        ' - there are better alternatives. This is an example of easy.
        Dim RenderThread As New Thread(AddressOf _renderloop)


        ' MTA or bust. If not MTA then it isn't really Multithreaded.
        If Not RenderThread.TrySetApartmentState(ApartmentState.MTA) Then
            MsgBox("Some limitation on your PC prevents me from runing Multithreaded!")
        End If

        ' Fire Away.
        RenderThread.Start()
    End Sub



    ' In this routine
    ' -- we configure the form, the buffer, drawing and target surface 
    ' -- loop until form closes
    ' -- raise our paint event were the actual painting of game graphics will take place
    ' -- disposes our surfaces and buffer when we are done with them
    ' All in one function
    ' Does not allow dynamic resizing. That you can experiment with.
    Private Sub _renderloop()
 
        ' Alternatively for fullscreen ( Fake Fullscreen ) 
        ' --> dont set client size , but instead set:
        ' --> Borderstyle: None
        ' --> Windowstate: Maximized
        ' --> OnTop: True  [ Or use WIN32 API calls to force Always On Top  ] 


        ' Create a backwash - er Backbuffer and some surfaces:
        Dim B_BUFFER = New Bitmap(Me.ClientSize.Width, Me.ClientSize.Height) ' backbuffer
        Dim G_BUFFER = Graphics.FromImage(B_BUFFER) 'drawing surface
        Dim G_TARGET = Me.CreateGraphics ' target surface

        ' Clear the random gibberish that would have been behind (and now imprinted in) the form away. 
        G_TARGET.Clear(Color.SlateGray)

        ' Configure Surfaces for optimal rendering:
        With G_TARGET ' Display 
            .CompositingMode = Drawing2D.CompositingMode.SourceCopy
            .CompositingQuality = Drawing2D.CompositingQuality.AssumeLinear
            .SmoothingMode = Drawing2D.SmoothingMode.None
            .InterpolationMode = Drawing2D.InterpolationMode.NearestNeighbor
            .TextRenderingHint = Drawing.Text.TextRenderingHint.SystemDefault
            .PixelOffsetMode = Drawing2D.PixelOffsetMode.HighSpeed
        End With

        With G_BUFFER ' Backbuffer

            ' Antialiased Polygons and Text?
            If Antialiasing Then
                .SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
                .TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
            Else
                ' defaults will not smooth
            End If

            .CompositingMode = Drawing2D.CompositingMode.SourceOver
            .CompositingQuality = Drawing2D.CompositingQuality.HighSpeed
            .InterpolationMode = Drawing2D.InterpolationMode.Low
            .PixelOffsetMode = Drawing2D.PixelOffsetMode.Half
        End With


        ' The Loop, terminates automatically when the window is closing.
        While Me.Disposing = False And Me.IsDisposed = False And Me.Visible = True

            ' we use an exception handler because sometimes the form may be
            ' - beginning to unload after the above checks. 
            ' - Most exceptions I get here are during the unloading stage.
            ' - Or attempting to draw to a window that has probably begun unloading already .
            ' - also any errors within OnPaint are ignored. 
            ' - Use Exception handlers within OnPaint()
            Try

                ' Raise the Paint Event - were the drawing code will go
                RaiseEvent Paint(G_BUFFER)

                ' Update Window using the fastest available GDI function to do it with.
                With G_TARGET
                    .DrawImageUnscaled(B_BUFFER, 0, 0)
                End With

            Catch E As Exception
                ' Show me what happened in the debugger
                ' Note: Too Many exception handlers can cause JIT to slow down your renderloop. 
                ' - One should be enough. Stack Trace (usually) tells all!
#If DEBUG Then
                Debug.Print(E.ToString)
#End If
            End Try

        End While

        ' If we are here then the window is closing or has closed. 
        ' - Causing the loop to end

        ' Clean up:
        G_TARGET.Dispose()
        G_BUFFER.Dispose()
        B_BUFFER.Dispose()

        ' Routine is done. K THX BYE
    End Sub



    ''' <summary>
    ''' This is were we mess with the paint.
    ''' </summary>
    ''' <param name="G"></param>
    ''' <remarks></remarks>
    Private Sub frmMain_Paint(ByVal G As System.Drawing.Graphics) Handles Me.Paint
        With G
            ' clear every frame ( only if you need to )
            G.Clear(Color.SlateGray)

            .DrawString(_Message, _Font, New SolidBrush(Color.FromArgb(100, Color.Black)), 22, 22)
            .DrawString(_Message, _Font, Brushes.SkyBlue, 20, 20)
 
            .DrawString("Frame " & _counter & " - FPS: " & _fps, _Font, Brushes.Lime, 20, 80)

            ' We are not rendering much - so its only natural to have high FPS (for now )


            .DrawString( _
                   "Client Area: " & ClientRectangle.ToString & " / Window: " & Bounds.ToString & _
                   " / Mode: " & Thread.CurrentThread.GetApartmentState.ToString, _
                     SystemFonts.DefaultFont, Brushes.Yellow, 4, 4)

            _counter += 1 ' total frames 
            _fpsstep += 1 ' step for FPS count per second

            ' just in case the thread happens to not be running as MTA
            If Thread.CurrentThread.GetApartmentState = ApartmentState.STA Then
                ' oh crap. Single threaded. 
                ' We need this:
                Application.DoEvents()
            End If
        End With
    End Sub

    Private Sub _fpstimer_Tick(ByVal sender As Object, ByVal e As System.EventArgs) Handles _fpstimer.Tick
        _fps = _fpsstep
        _fpsstep = 0
    End Sub
End Class
