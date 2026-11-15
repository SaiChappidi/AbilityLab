# Dex run

Run type: Intentional mel-nova fixture reset

Result: Failed — 

Elapsed: 1.9 seconds. No manual baseline has been measured.

Compilation failed. Source and backups preserved.
Assets\Dex\Editor\AgentWorkbench.cs(139,34): error CS0103: The name 'ManualBaseline' does not exist in the current context
Assets\Dex\Editor\AgentWorkbench.cs(210,58): error CS0103: The name 'ManualBaseline' does not exist in the current context
Assets\Dex\Editor\WorkbenchDashboard.cs(45,65): error CS0103: The name 'ManualBaseline' does not exist in the current context
Assets\Dex\Editor\WorkbenchDashboard.cs(106,115): error CS0103: The name 'ManualBaseline' does not exist in the current context
Assets\Dex\Editor\WorkbenchDashboard.cs(107,37): error CS0103: The name 'ManualBaseline' does not exist in the current context
Assets\Dex\Editor\WorkbenchDashboard.cs(107,157): error CS0103: The name 'ManualBaseline' does not exist in the current context
Assets\Dex\Editor\WorkbenchDashboard.cs(107,179): error CS0103: The name 'ManualBaseline' does not exist in the current context
Assets\Dex\Editor\WorkbenchDashboard.cs(107,215): error CS0103: The name 'ManualBaseline' does not exist in the current context

## Timeline

0.0s  /  Investigating
0.0s  /  Repairing
0.0s  /  Compiling
1.9s  /  Failed

## Exploration



The agent has read-only tools. The workbench applies validated expressions and verifies them independently. See source diffs, verifier hashes, CLI event transcripts and before/after replay captures in this directory.