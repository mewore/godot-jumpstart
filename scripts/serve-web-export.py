import http.server
import socketserver
import os
import sys

current_dir = os.path.dirname(__file__)
project_dir = os.path.join(current_dir, '..')
exported_web_dir = os.path.join(project_dir, 'export', sys.argv[1], 'web')
os.chdir(exported_web_dir)

PORT = 8000

handler = http.server.SimpleHTTPRequestHandler
http_daemon = socketserver.TCPServer(('', PORT), handler)
print('Go to: http://localhost:' + str(PORT))
http_daemon.serve_forever()
