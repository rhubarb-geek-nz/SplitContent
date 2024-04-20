#!/usr/bin/env pwsh
# Copyright (c) 2024 Roger Brown.
# Licensed under the MIT License.

trap
{
	throw $PSItem
}

$ErrorActionPreference = 'Stop'
$InformationPreference = 'Continue'

Get-Command -Name 'Split-Content'

$lines = Split-Content -LiteralPath 'test.ps1'

$lines.Count
$lines[$lines.Count-1]
# The End
