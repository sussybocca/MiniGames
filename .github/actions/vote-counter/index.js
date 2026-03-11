const core = require('@actions/core');
const github = require('@actions/github');

async function run() {
  try {
    const token = core.getInput('repo-token');
    const threshold = parseInt(core.getInput('threshold'), 10);
    const octokit = github.getOctokit(token);
    const context = github.context;

    // Fetch all open issues (excluding pull requests)
    const { data: issues } = await octokit.rest.issues.listForRepo({
      owner: context.repo.owner,
      repo: context.repo.repo,
      state: 'open',
      per_page: 100,
    });

    let updatedCount = 0;

    for (const issue of issues) {
      if (issue.pull_request) continue; // skip PRs

      // Get the number of 👍 reactions
      const { data: reactions } = await octokit.rest.reactions.listForIssue({
        owner: context.repo.owner,
        repo: context.repo.repo,
        issue_number: issue.number,
        content: '+1',
      });

      const voteCount = reactions.length;

      // Determine desired labels
      const currentLabels = issue.labels.map(l => l.name);
      let newLabels = [...currentLabels];

      // Remove any existing priority labels
      newLabels = newLabels.filter(l => !l.startsWith('priority:'));

      // Add new label based on vote count
      if (voteCount >= threshold) {
        newLabels.push('priority: high-demand');
      } else if (voteCount >= 3) {
        newLabels.push('priority: medium-demand');
      } else {
        newLabels.push('priority: low-demand');
      }

      // Only update if labels changed
      if (JSON.stringify(currentLabels.sort()) !== JSON.stringify(newLabels.sort())) {
        await octokit.rest.issues.setLabels({
          owner: context.repo.owner,
          repo: context.repo.repo,
          issue_number: issue.number,
          labels: newLabels,
        });
        updatedCount++;
      }
    }

    core.setOutput('updated-issues', updatedCount);
    console.log(`Updated ${updatedCount} issues with new priority labels.`);

  } catch (error) {
    core.setFailed(error.message);
  }
}

run();
