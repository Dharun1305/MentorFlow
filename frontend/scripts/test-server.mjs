import { createServer } from 'node:http';
import { readFile, stat } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

export async function startFrontend() {
  const root = path.resolve(fileURLToPath(new URL('../dist/mentorflow/browser/', import.meta.url)));
  const mime = { '.js': 'text/javascript', '.css': 'text/css', '.svg': 'image/svg+xml', '.html': 'text/html' };
  const server = createServer(async (request, response) => {
    try {
      let file = path.resolve(root, `.${new URL(request.url, 'http://localhost').pathname}`);
      if (!file.startsWith(root + path.sep)) file = path.join(root, 'index.html');
      try { if (!(await stat(file)).isFile()) file = path.join(root, 'index.html'); }
      catch { file = path.join(root, 'index.html'); }
      response.setHeader('Content-Type', mime[path.extname(file)] ?? 'application/octet-stream');
      response.end(await readFile(file));
    } catch { response.writeHead(500).end(); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  return { base: `http://127.0.0.1:${server.address().port}`, close: () => new Promise(resolve => server.close(resolve)) };
}
