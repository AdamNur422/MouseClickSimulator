   M S B A M L   `   `   `  ���� P  LTTMouseclickSimulator, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null=#clr-namespace:TTMouseClickSimulatorTTMouseClickSimulator  &     TTMouseClickSimulator.MainWindow   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     @d2http://schemas.microsoft.com/expression/blend/2008     Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5local#clr-namespace:TTMouseClickSimulator     -     � Title$  AppName��$	��540��$	��542��&   System.Windows.Media.TextOptions   TextFormattingMode$ Display=� � UseLayoutRounding$
 True��.��� .+��� -    ��Name$ btnStart��$��Start��$
��Left=�$��	10,0,0,10q�$��75��$��21��$��Bottom=��� -   $ btnStop��${�False��$
��Stop��$
��Left=�$��
110,0,0,10q�$��75��$��21��$��Bottom=��� -    ��Name$ 
lblAppName��$��AppName��$��
10,10,10,0q�$	��Top=�$
��Left=�$��18(��� -   $ btnLoad��$��Load XML Project file…��$
��Left=�$��	10,50,0,0q�$	��Top=�$	��154��$��21��� $��10,76,10,80q�.+�� -    �Name$ gridProjectControls�� � RowDefinitions �� $
��Auto ��� $
��Auto ��� $
��Auto ��� $
��Auto ��� .+��� -    ��Name$ txtCurrentProject��$��0��$
��Wrap=�$
��xxxx��$
��Left=�$	��Top=�$��6q�$��13.333(��� -   $ txtDescription��$��1��$
��Wrap=�$��TextBlock
NewLine��$
��Left=�$	��Top=�$��6q��� -	   $ lblActionTitle��$��3��$��lblActionTitle��$
��Left=�$	��Top=�$��SemiBold"�$��
0, 3, 0, 0q�$��13.333(�� $��4��.+��� $��2��$��0,0,0,0q�$
��Auto=�.��� -
   $ actionListGrid���� -    ��Name$ overlayMessageBorder��
���   �$��1q����	#F2E6E6E6$��40,20q�	 �� CornerRadius$	 10��$	��120��$y�Hidden=�._��� -   $ overlayMessageTextBlock��$��5,5,5,5q�$��16(�$��WrapWithOverflow=��� -   $  textBlockStopSimulatorNote��$��
212,0,10,0q�$
��Wrap=�$9��3Note: To stop the Simulator, press the Windows key.��$��31��$��Bottom=��� -   
 ��Name$
 chkUseWasdMovement��$��	10,0,0,56q�$1��+Use WASD instead of arrow keys for movement��$��15��$��Bottom=�$
��Left=� ^� ShowDuration$ 60000�� � ToolTip _� .���� rpEnable this option when you have set W, A, S, D keys instead of arrow keys for movement in the controls options.�� �� XVThis will automatically convert arrow keys as specified in project files to WASD keys.�� -   $
 chkEnableBackgroundMode��$��	10,0,0,36q�$��Enable Background Mode��$��15��$��Bottom=�$
��Left=�$ 60000�� _� .���� ��When using background mode, the simulator directly sends mouse and keyboard inputs to the Toontown window (instead of simulating gobal inputs), so you can do other work while the simulator is running.�� �� ��The Toontown window can even be hidden behind other apps. However, when using mouse actions (like auto fishing), you shouldn't move the mouse pointer into the Toontown window while the simulator is running.�� �� |zIf this mode doesn't work correctly on your machine (e.g. toon gets sleepy), please disable background mode and try again.