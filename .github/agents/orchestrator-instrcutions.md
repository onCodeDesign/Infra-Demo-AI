The agent follows the exact flow described in @file:FeatureWorkflow.json. Use loops, graphs and all the modern agentic ai, but strictly follow the specs.
The flow in the json file becomes the flow in the agent.md file.

This agent is meant to replace the cFabric orchestrator that has its setup and state management in `.cFabric` folder
The agent manages spawns and hands over tasks and transfers context to the appropriate agents as described in the workflow. 
Each spawned agent works in its own separate context. They all put their outputs on files as their instructions dictate.

Name it FeatureWorkflow-orchestrator

Inputs: 
 - issue number to implement; - required
 - a short name of the feature (needed for the git feature name) (optional: infer if not given)
 - mode: new|resume old instance
The agent should be able to resume from the last snapshot in case of interruptions or failures.
Make the flow resumable. This means that after each step of each phase, a snapshot is written while running - as cFabric does it in `.cFabric\workflow` folder

 The agent also manages the git branching for the feature it implements:
  - it creates a feature\{issueId}-{shortname}\main feature at start
   - when a phase starts it creates feature\{issueId}-{shortname}\{phasename}
      - each spawned agent commits on the phase branch
   - when a phase ends it merges the phase branch back into the main feature branch using a --no-ff merge 
Existent `cFabric-demo/feature/#1-overdue-orders/main`; `cFabric-demo/feature/#1-overdue-orders/design` and `cFabric-demo/feature/#1-overdue-orders/implement` are a good example for this.