import { useState, useRef } from 'react';
import { useAuth } from '../contexts/AuthContext';
import { useNavigate } from 'react-router-dom';
import { requestUploadUrl, uploadFileToS3, type UploadProgress } from '../api/upload';

const ALLOWED_TYPES = ['.mp4', '.avi', '.mkv', '.mov', '.webm'];

export default function HomePage() {
  const { user, accessToken, logout } = useAuth();
  const navigate = useNavigate();
  const inputRef = useRef<HTMLInputElement>(null);

  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const [progress, setProgress] = useState<UploadProgress | null>(null);
  const [publicUrl, setPublicUrl] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  function handleLogout() {
    logout();
    navigate('/login');
  }

  function handleFileSelect(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    const ext = file.name.toLowerCase().slice(file.name.lastIndexOf('.'));
    if (!ALLOWED_TYPES.includes(ext)) {
      setError(`Unsupported format. Allowed: ${ALLOWED_TYPES.join(', ')}`);
      setSelectedFile(null);
      return;
    }

    setError(null);
    setPublicUrl(null);
    setProgress(null);
    setSelectedFile(file);
  }

  async function handleUpload() {
    if (!selectedFile || !accessToken) return;

    setUploading(true);
    setError(null);
    setPublicUrl(null);

    try {
      // Step 1: get presigned URL from backend
      const { uploadUrl, publicUrl: pubUrl } = await requestUploadUrl(accessToken, {
        fileName: selectedFile.name,
        contentType: selectedFile.type || 'application/octet-stream',
      });

      // Step 2: upload directly to S3/MinIO
      await uploadFileToS3(uploadUrl, selectedFile, setProgress);

      setPublicUrl(pubUrl);
      setSelectedFile(null);
      if (inputRef.current) inputRef.current.value = '';
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  }

  if (!user) {
    return (
      <div className="page">
        <h1>VideoFlow</h1>
        <p>Video hosting platform.</p>
        <div className="actions">
          <button onClick={() => navigate('/login')}>Sign In</button>
          <button onClick={() => navigate('/register')}>Create Account</button>
        </div>
      </div>
    );
  }

  return (
    <div className="page">
      <header className="header">
        <h1>VideoFlow</h1>
        <div className="user-info">
          <span>{user.email}</span>
          <button onClick={handleLogout}>Sign Out</button>
        </div>
      </header>

      <main>
        <p className="welcome">Welcome, {user.email}!</p>

        <div className="upload-card">
          <label className="file-label">
            {selectedFile ? (
              <span className="file-name">{selectedFile.name}</span>
            ) : (
              <span className="file-placeholder">Select a video file</span>
            )}
            <input
              ref={inputRef}
              type="file"
              accept={ALLOWED_TYPES.join(',')}
              onChange={handleFileSelect}
              disabled={uploading}
              hidden
            />
          </label>

          {error && <div className="error">{error}</div>}

          {progress && (
            <div className="progress-bar-wrapper">
              <div className="progress-bar">
                <div
                  className="progress-fill"
                  style={{ width: `${progress.percent}%` }}
                />
              </div>
              <span className="progress-text">{progress.percent}%</span>
            </div>
          )}

          <button
            onClick={handleUpload}
            disabled={!selectedFile || uploading}
            className="upload-btn"
          >
            {uploading ? 'Uploading...' : 'Upload'}
          </button>

          {publicUrl && (
            <div className="success">
              ✅ Upload complete!
              <br />
              <a href={publicUrl} target="_blank" rel="noopener noreferrer">
                Open video
              </a>
            </div>
          )}
        </div>
      </main>
    </div>
  );
}