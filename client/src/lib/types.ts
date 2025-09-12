import type { ErrorResponse, InternalErrorResponse } from "$lib/api";

export interface APIError {
	type: string;
	title: string;
	status: number;
	errors?: APIErrors;
}

export interface APIErrors {
	Id: string[];
}

export interface ValidationError {
	statusCode: string;
	message: string;
	errors?: ValidationErrorItem;
}

interface ValidationErrorItem {
	[key: string]: string[];
}

export interface ErrorGeneric {
	statusCode: number;
	message: string;
	summary: string;
	errors: ErrorGenericErrors;
}

interface ErrorGenericErrors {
	[key: string]: string[];
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isInternalErrorResponse = (error: any): error is InternalErrorResponse => {
	return "status" in error && "code" in error && "reason" in error;
};

export const isErrorResponse = (
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	error: any
): error is ErrorResponse => {
	return "statusCode" in error && "message" in error && "errors" in error;
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export function isError(err: any | unknown): err is Error {
	return (err as Error).message !== undefined;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const isValidationError = (apiError: any): apiError is ValidationError => {
	return "statusCode" in apiError && "message" in apiError && "errors" in apiError;
};

export interface UserDTO {
	id: number;
	name: string;
	type: number;
}

export interface User {
	id: number;
	name: string;
	token: string;
	roles: Array<string>;
}

export interface HubNotification {
	notification: string;
	dateSent: string;
}

export interface UserToken {
	token: string;
}

export type ApiError = ErrorResponse | InternalErrorResponse | Error | unknown | void;

export const getError = (inputError: ApiError): ErrorGeneric => {
	const error: ErrorGeneric = {
		statusCode: 0,
		message: "",
		summary: "",
		errors: {},
	};

	if (inputError == null) {
		return error;
	}

	if (isErrorResponse(inputError)) {
		error.message = inputError.message;
		error.statusCode = inputError.statusCode;
		error.errors = inputError.errors;
		error.summary = Object.values(inputError.errors)
			.map((err) => {
				return err.join();
			})
			.join();
	} else if (isInternalErrorResponse(inputError)) {
		error.message = inputError.reason;
		error.statusCode = inputError.code;
		error.summary = inputError.note;
		error.errors = {
			code: [inputError.code.toString()],
			status: [inputError.status],
			reason: [inputError.reason],
			note: [inputError.note],
		};
	} else if (isError(inputError)) {
		const summary = inputError.cause as string | undefined;
		error.message = summary ?? inputError.message;
		error.statusCode = 0;
		error.summary = inputError.message;
		error.errors = {
			name: [inputError.name],
			cause: [inputError.cause as string],
			stack: [inputError.stack as string],
		};
	} else {
		error.message = "An unknown error occurred";
	}
	return error;
};

export const enum LinkType {
	MenuItem,
	Extra,
	Option,
	Bulk,
}
