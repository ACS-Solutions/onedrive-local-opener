import * as esbuild from 'esbuild';

await esbuild.build({
  entryPoints: {
    content: 'src/content.ts',
    background: 'src/background.ts',
    options: 'src/options.ts',
    popup: 'src/popup.ts',
    handler: 'src/handler.ts',
  },
  outdir: 'dist',
  bundle: true,
  // minify: true,
  platform: 'browser',
  target: 'es2020',
  logLevel: 'info',
});
