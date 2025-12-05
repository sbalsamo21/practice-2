// API Configuration
function getApiUrl() {
    const apiUrlInput = document.getElementById('apiUrl');
    return apiUrlInput ? apiUrlInput.value.trim() || 'http://localhost:5268/task' : 'http://localhost:5268/task';
}

// Message handling
function showMessage(message, type = 'error') {
    const container = document.getElementById('messageContainer');
    if (!container) return;

    container.innerHTML = `<div class="message ${type}">${escapeHtml(message)}</div>`;
    
    setTimeout(() => {
        container.innerHTML = '';
    }, 5000);
}

// Escape HTML to prevent XSS
function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// Format date for display
function formatDate(dateString) {
    if (!dateString) return 'N/A';
    try {
        const date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } catch (e) {
        return 'Invalid Date';
    }
}

// Get status badge class
function getStatusClass(status) {
    if (!status) return 'status-not-started';
    const normalized = status.toLowerCase().replace(/\s+/g, '-');
    return `status-${normalized}`;
}

// Get priority badge class
function getPriorityClass(priority) {
    if (!priority) return 'priority-low';
    const normalized = priority.toLowerCase();
    return `priority-${normalized}`;
}

// Load all tasks
async function loadTasks() {
    const loadingMessage = document.getElementById('loadingMessage');
    const tableContainer = document.getElementById('tasksTableContainer');
    const emptyState = document.getElementById('emptyState');
    const tableBody = document.getElementById('tasksTableBody');

    if (loadingMessage) loadingMessage.style.display = 'block';
    if (tableContainer) tableContainer.style.display = 'none';
    if (emptyState) emptyState.style.display = 'none';

    try {
        const apiUrl = getApiUrl();
        const response = await fetch(apiUrl);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const tasks = await response.json();

        if (loadingMessage) loadingMessage.style.display = 'none';

        if (!tasks || tasks.length === 0) {
            if (emptyState) emptyState.style.display = 'block';
            return;
        }

        if (tableBody) {
            tableBody.innerHTML = '';
            tasks.forEach(task => {
                const row = createTaskRow(task);
                tableBody.appendChild(row);
            });
        }

        if (tableContainer) tableContainer.style.display = 'block';
    } catch (error) {
        if (loadingMessage) loadingMessage.style.display = 'none';
        showMessage(`Failed to load tasks: ${error.message}. Make sure your API is running at ${getApiUrl()}`, 'error');
        console.error('Error loading tasks:', error);
    }
}

// Create table row for a task
function createTaskRow(task) {
    const row = document.createElement('tr');
    
    const statusClass = getStatusClass(task.status);
    const priorityClass = getPriorityClass(task.priority);
    
    row.innerHTML = `
        <td>${task.taskId || 'N/A'}</td>
        <td><strong>${escapeHtml(task.name || '')}</strong></td>
        <td>${escapeHtml(task.description || '')}</td>
        <td><span class="status-badge ${statusClass}">${escapeHtml(task.status || 'Not Set')}</span></td>
        <td><span class="priority-badge ${priorityClass}">${escapeHtml(task.priority || 'Not Set')}</span></td>
        <td>${formatDate(task.createdAt)}</td>
        <td>
            <div class="action-buttons">
                <button class="btn btn-warning btn-small" onclick="editTask(${task.taskId})">Edit</button>
                <button class="btn btn-danger btn-small" onclick="deleteTask(${task.taskId})">Delete</button>
            </div>
        </td>
    `;
    
    return row;
}

// Reset form
function resetForm() {
    const form = document.getElementById('taskForm');
    if (form) {
        form.reset();
    }
    
    const taskIdInput = document.getElementById('taskId');
    if (taskIdInput) {
        taskIdInput.value = '';
    }
    
    const formTitle = document.getElementById('formTitle');
    if (formTitle) {
        formTitle.textContent = 'Add New Task';
    }
    
    const submitBtn = document.getElementById('submitBtn');
    if (submitBtn) {
        submitBtn.textContent = 'Add Task';
    }
    
    const cancelBtn = document.getElementById('cancelBtn');
    if (cancelBtn) {
        cancelBtn.style.display = 'none';
    }
}

// Edit task
async function editTask(taskId) {
    try {
        const apiUrl = getApiUrl();
        const response = await fetch(`${apiUrl}/${taskId}`);

        if (!response.ok) {
            if (response.status === 404) {
                throw new Error('Task not found');
            }
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const task = await response.json();

        // Populate form
        const taskIdInput = document.getElementById('taskId');
        const taskNameInput = document.getElementById('taskName');
        const taskDescriptionInput = document.getElementById('taskDescription');
        const taskStatusInput = document.getElementById('taskStatus');
        const taskPriorityInput = document.getElementById('taskPriority');
        const formTitle = document.getElementById('formTitle');
        const submitBtn = document.getElementById('submitBtn');
        const cancelBtn = document.getElementById('cancelBtn');

        if (taskIdInput) taskIdInput.value = task.taskId || '';
        if (taskNameInput) taskNameInput.value = task.name || '';
        if (taskDescriptionInput) taskDescriptionInput.value = task.description || '';
        if (taskStatusInput) taskStatusInput.value = task.status || '';
        if (taskPriorityInput) taskPriorityInput.value = task.priority || '';
        if (formTitle) formTitle.textContent = 'Edit Task';
        if (submitBtn) submitBtn.textContent = 'Update Task';
        if (cancelBtn) cancelBtn.style.display = 'inline-block';

        // Scroll to form
        const formSection = document.querySelector('.form-section');
        if (formSection) {
            formSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }

        showMessage('Task loaded for editing', 'success');
    } catch (error) {
        showMessage(`Failed to load task for editing: ${error.message}`, 'error');
        console.error('Error loading task:', error);
    }
}

// Delete task
async function deleteTask(taskId) {
    if (!confirm('Are you sure you want to delete this task? This action cannot be undone.')) {
        return;
    }

    try {
        const apiUrl = getApiUrl();
        const response = await fetch(`${apiUrl}/${taskId}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            if (response.status === 404) {
                throw new Error('Task not found');
            }
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        showMessage('Task deleted successfully!', 'success');
        await loadTasks();
    } catch (error) {
        showMessage(`Failed to delete task: ${error.message}`, 'error');
        console.error('Error deleting task:', error);
    }
}

// Handle form submission
async function handleFormSubmit(event) {
    event.preventDefault();

    const taskIdInput = document.getElementById('taskId');
    const taskNameInput = document.getElementById('taskName');
    const taskDescriptionInput = document.getElementById('taskDescription');
    const taskStatusInput = document.getElementById('taskStatus');
    const taskPriorityInput = document.getElementById('taskPriority');

    const taskId = taskIdInput ? taskIdInput.value : '';
    const taskName = taskNameInput ? taskNameInput.value.trim() : '';
    const taskDescription = taskDescriptionInput ? taskDescriptionInput.value.trim() : '';
    const taskStatus = taskStatusInput ? taskStatusInput.value : '';
    const taskPriority = taskPriorityInput ? taskPriorityInput.value : '';

    // Validation
    if (!taskName) {
        showMessage('Task name is required', 'error');
        return;
    }

    if (!taskStatus) {
        showMessage('Task status is required', 'error');
        return;
    }

    if (!taskPriority) {
        showMessage('Task priority is required', 'error');
        return;
    }

    const taskData = {
        name: taskName,
        description: taskDescription || '',
        status: taskStatus,
        priority: taskPriority
    };

    try {
        const apiUrl = getApiUrl();
        let response;

        if (taskId) {
            // Update existing task
            response = await fetch(`${apiUrl}/${taskId}`, {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(taskData)
            });
        } else {
            // Create new task
            response = await fetch(apiUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(taskData)
            });
        }

        if (!response.ok) {
            const errorData = await response.json().catch(() => ({ message: 'Unknown error' }));
            throw new Error(errorData.message || `HTTP error! status: ${response.status}`);
        }

        const result = await response.json();
        showMessage(taskId ? 'Task updated successfully!' : 'Task created successfully!', 'success');
        resetForm();
        await loadTasks();
    } catch (error) {
        showMessage(`Failed to save task: ${error.message}`, 'error');
        console.error('Error saving task:', error);
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', () => {
    // Load tasks
    loadTasks();

    // Set up form submission
    const taskForm = document.getElementById('taskForm');
    if (taskForm) {
        taskForm.addEventListener('submit', handleFormSubmit);
    }

    // Set up cancel button
    const cancelBtn = document.getElementById('cancelBtn');
    if (cancelBtn) {
        cancelBtn.addEventListener('click', resetForm);
    }

    // Make functions available globally for onclick handlers
    window.editTask = editTask;
    window.deleteTask = deleteTask;
});

