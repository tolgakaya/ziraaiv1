import os
import re

def fix_tracking_issues(directory):
    """Find and fix GetAsync -> Update/Delete patterns"""
    fixed_files = []

    for root, dirs, files in os.walk(directory):
        for file in files:
            if not file.endswith('.cs'):
                continue

            filepath = os.path.join(root, file)

            try:
                with open(filepath, 'r', encoding='utf-8') as f:
                    content = f.read()

                # Skip if no GetAsync or no Update/Delete
                if '.GetAsync' not in content:
                    continue
                if '.Update(' not in content and '.Delete(' not in content:
                    continue

                original_content = content

                # Pattern: Find variable assignments with GetAsync
                # var something = await _repo.GetAsync(...)
                pattern = r'(var\s+(\w+)\s*=\s*await\s+[^;]+\.GetAsync\([^)]+\);)'

                matches = list(re.finditer(pattern, content))

                for match in matches:
                    var_name = match.group(2)
                    get_line = match.group(1)

                    # Check if this variable is used in Update or Delete within next 500 chars
                    start_pos = match.end()
                    check_region = content[start_pos:start_pos + 500]

                    if f'.Update({var_name})' in check_region or f'.Delete({var_name})' in check_region:
                        # Replace GetAsync with GetTrackedAsync
                        new_line = get_line.replace('.GetAsync(', '.GetTrackedAsync(')
                        content = content.replace(get_line, new_line)
                        print(f"Fixed in {filepath}: {var_name}")

                # Only write if changes were made
                if content != original_content:
                    with open(filepath, 'w', encoding='utf-8') as f:
                        f.write(content)
                    fixed_files.append(filepath)

            except Exception as e:
                print(f"Error processing {filepath}: {e}")

    return fixed_files

if __name__ == '__main__':
    handlers_dir = r'C:\Users\Asus\Documents\Visual Studio 2022\ziraai\Business\Handlers'
    services_dir = r'C:\Users\Asus\Documents\Visual Studio 2022\ziraai\Business\Services'

    print("Scanning Handlers for GetAsync -> Update/Delete patterns...")
    fixed_handlers = fix_tracking_issues(handlers_dir)

    print(f"\n\nFixed {len(fixed_handlers)} Handler files:")
    for f in fixed_handlers:
        print(f"  - {f}")

    print("\n\nScanning Services for GetAsync -> Update/Delete patterns...")
    fixed_services = fix_tracking_issues(services_dir)

    print(f"\n\nFixed {len(fixed_services)} Service files:")
    for f in fixed_services:
        print(f"  - {f}")

    total = len(fixed_handlers) + len(fixed_services)
    print(f"\n\n=== TOTAL: {total} files fixed ===")
