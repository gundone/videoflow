const UPLOAD_API = 'http://localhost:5000/api';

export interface PresignedUploadResult {
  fileId: string;
  uploadUrl: string;
  publicUrl: string;
  expiresAt: string;
}

export interface UploadProgress {
  loaded: number;
  total: number;
  percent: number;
}

/**
 * Step 1: Request a presigned URL from the Upload Service.
 */
export async function requestUploadUrl(
  token: string,
  data: { fileName: string; contentType: string },
): Promise<PresignedUploadResult> {
  const res = await fetch(`${UPLOAD_API}/upload/request`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(data),
  });

  if (!res.ok) {
    const err = await res.text();
    throw new Error(err || 'Failed to get upload URL');
  }

  return res.json();
}

/**
 * Step 2: Upload the file directly to S3/MinIO via the presigned URL.
 * Uses XMLHttpRequest for upload progress tracking.
 */
export function uploadFileToS3(
  uploadUrl: string,
  file: File,
  onProgress?: (p: UploadProgress) => void,
): Promise<void> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    xhr.open('PUT', uploadUrl, true);
    xhr.setRequestHeader('Content-Type', file.type);

    xhr.upload.onprogress = (e) => {
      if (e.lengthComputable && onProgress) {
        onProgress({
          loaded: e.loaded,
          total: e.total,
          percent: Math.round((e.loaded / e.total) * 100),
        });
      }
    };

    xhr.onload = () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        resolve();
      } else {
        reject(new Error(`Upload failed (HTTP ${xhr.status}: ${xhr.statusText})`));
      }
    };

    xhr.onerror = () => reject(new Error('Network error during upload'));
    xhr.send(file);
  });
}