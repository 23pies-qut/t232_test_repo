export function getFileExtension(fileName) {
  if (!fileName || !fileName.includes(".")) {
    return "FILE";
  }

  const extension = fileName
    .split(".")
    .pop()
    ?.toUpperCase();

  return extension ? extension.slice(0, 5) : "FILE";
}

export function formatFileSize(sizeBytes) {
  const size = Number(sizeBytes);

  if (!Number.isFinite(size) || size < 0) {
    return "Unknown";
  }

  if (size < 1024) {
    return `${size} B`;
  }

  if (size < 1024 * 1024) {
    return `${(size / 1024).toFixed(1)} KB`;
  }

  if (size < 1024 * 1024 * 1024) {
    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }

  return `${(size / (1024 * 1024 * 1024)).toFixed(1)} GB`;
}
